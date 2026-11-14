using Microsoft.Extensions.Options;
using Ritten.Commands;
using Ritten.Contracts;

namespace Ritten.NSchema;

/// <summary>
/// Runs <c>nschema</c> in the project's directory: the executable the options name, or the
/// project's local tool, restored first.
/// </summary>
/// <param name="commands">The command runner.</param>
/// <param name="options">How NSchema is run.</param>
/// <param name="secretProvider">The secrets store the host registered, for the references an env file names; none in a host with no secrets.</param>
internal sealed class NSchemaClient(ICommandRunner commands, IOptions<NSchemaOptions> options, ISecretProvider? secretProvider = null) : INSchema
{
    private readonly HashSet<string> _restored = [];

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>?> VerifyFormatting(NSchemaProject project, CancellationToken ct = default)
    {
        // `format` takes its path as an argument and ignores --directory, so it runs in the
        // project and is handed ".".
        var format = await commands.Run((await NSchema(project, null, ct, "format", "--check", ".", "--no-color")).QuietOutput(), ct);
        IReadOnlyList<string>? unformatted = format.ExitCode.Value switch
        {
            0 => null,
            2 => format.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            _ => throw new CommandFailedException($"nschema format exited with code {format.ExitCode}.", format)
        };

        // `format --check` passes over a file it cannot parse, so an invalid one would read as
        // formatted: validate says what is wrong with it.
        await commands.Run((await NSchema(project, null, ct, "validate", "--no-color")).AndArguments(NoInit()).ThrowOnError(), ct);
        return unformatted;
    }

    /// <inheritdoc />
    public async Task<NSchemaPlanResult> Plan(NSchemaProject project, NSchemaEnvironment? environment = null, CancellationToken ct = default)
    {
        // --detailed-exitcode splits the answer three ways: 0 nothing to do, 2 changes, anything
        // else broken; without it a plan with changes and one with none both exit 0.
        var command = await NSchema(project, environment, ct, "plan", "--detailed-exitcode", "--format", "markdown", "--no-color");
        var result = await commands.Run(command.AndArguments([.. Environment(environment), .. NoInit()]), ct);
        return result.ExitCode.Value switch
        {
            0 => new NSchemaPlanResult(false, result.StandardOutput),
            2 => new NSchemaPlanResult(true, result.StandardOutput),
            _ => throw new CommandFailedException($"nschema plan exited with code {result.ExitCode}.", result)
        };
    }

    /// <inheritdoc />
    public async Task<NSchemaApplyResult> Apply(NSchemaProject project, NSchemaEnvironment? environment = null, NSchemaApplySettings? settings = null,
        CancellationToken ct = default)
    {
        var command = await NSchema(project, environment, ct, "apply", "--auto-approve", "--no-color");
        var result = await commands.Run(command.AndArguments([.. Environment(environment), .. NoInit(), .. Settings(settings)]).ThrowOnError(), ct);
        return new NSchemaApplyResult(result.StandardOutput);
    }

    private static string[] Environment(NSchemaEnvironment? environment) =>
        environment?.Name is { Length: > 0 } name ? ["--environment", name] : [];

    private string[] NoInit() => options.Value.NoInit ? ["--no-init"] : [];

    private static string[] Settings(NSchemaApplySettings? settings) =>
    [
        .. settings?.NoLock == true ? ["--no-lock"] : Array.Empty<string>(),
        .. settings?.DestructiveActions is { } policy ? ["--destructive-actions", policy.ToString().ToLowerInvariant()] : Array.Empty<string>()
    ];

    // The command in the project's directory, under the environment's variables: the env files
    // are read as each command starts and travel on the command, not the process.
    private async Task<Command> NSchema(NSchemaProject project, NSchemaEnvironment? environment, CancellationToken ct, params string[] arguments)
    {
        var command = options.Value.Executable is { Length: > 0 } executable
            ? Command.Create(executable).WithArguments(arguments)
            : Command.Create("dotnet").WithArguments(["nschema", .. arguments]);
        if (project.Directory is { Length: > 0 } directory)
        {
            command = command.InDirectory(directory);
        }

        if (options.Value.Executable is not { Length: > 0 })
        {
            await Restore(project, ct);
        }

        return await Variables(environment, ct) is { Count: > 0 } variables ? command.WithEnvironmentVariables(variables) : command;
    }

    // The local tool at the version the manifest pins, once per project: the manifest is found
    // from the project's directory upwards, as `dotnet nschema` finds it.
    private async Task Restore(NSchemaProject project, CancellationToken ct)
    {
        if (!_restored.Add(project.Directory ?? ""))
        {
            return;
        }

        var restore = Command.Create("dotnet").WithArguments("tool", "restore").QuietOutput().ThrowOnError();
        await commands.Run(project.Directory is { Length: > 0 } directory ? restore.InDirectory(directory) : restore, ct);
    }

    private async Task<IReadOnlyDictionary<string, string>> Variables(NSchemaEnvironment? environment, CancellationToken ct)
    {
        if (environment is null)
        {
            return new Dictionary<string, string>();
        }

        var variables = new Dictionary<string, string>();
        if (environment.EnvFiles.Count > 0)
        {
            var provider = secretProvider ?? throw new InvalidOperationException("An env file needs a secrets store, and this host registered none.");
            var loaded = await EnvironmentFile.Load(environment.EnvFiles, provider, ct);
            if (loaded.IsError)
            {
                throw new InvalidOperationException(string.Join(' ', loaded.Errors.Select(e => e.Message)));
            }

            foreach (var (name, value) in loaded.Value)
            {
                variables[name] = value;
            }
        }

        foreach (var (name, value) in environment.Variables)
        {
            variables[name] = value;
        }

        return variables;
    }
}
