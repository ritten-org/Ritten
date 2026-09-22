using Microsoft.Extensions.Options;
using Ritten.Commands;
using Ritten.Contracts;

namespace Ritten.OpenTofu;

/// <summary>
/// Runs the <c>tofu</c> on the path against the root module.
/// </summary>
/// <param name="commands">The command runner.</param>
/// <param name="secretProvider">The secrets store the host registered, for the references an env file names.</param>
/// <param name="options">Where the root module is.</param>
internal sealed class OpenTofuClient(ICommandRunner commands, ISecretProvider secretProvider, IOptions<OpenTofuOptions> options) : IOpenTofu
{
    /// <inheritdoc />
    public async Task Init(TofuEnvironment? environment = null, CancellationToken ct = default) =>
        await commands.Run((await Tofu(environment, ct, "init", "-input=false")).AndArguments(VarFile(environment)).ThrowOnError(), ct);

    /// <inheritdoc />
    public async Task<TofuPlanResult> Plan(TofuEnvironment? environment = null, CancellationToken ct = default)
    {
        // -detailed-exitcode splits the answer three ways: 0 nothing to do, 2 changes, anything
        // else broken. Without it a plan that cannot reach its backend and a plan with nothing
        // to do are the same exit code, and a drift check built on that reports quiet either way.
        var command = await Tofu(environment, ct, "plan", "-input=false", "-lock=false", "-no-color", "-detailed-exitcode");
        var result = await commands.Run(command.AndArguments(VarFile(environment)), ct);

        return result.ExitCode.Value switch
        {
            0 => new TofuPlanResult(false, result.StandardOutput),
            2 => new TofuPlanResult(true, result.StandardOutput),
            _ => throw new CommandFailedException($"tofu plan exited with code {result.ExitCode}.", result)
        };
    }

    /// <inheritdoc />
    public async Task Apply(TofuEnvironment? environment = null, CancellationToken ct = default) =>
        await commands.Run(
            (await Tofu(environment, ct, "apply", "-input=false", "-auto-approve", "-no-color")).AndArguments(VarFile(environment)).ThrowOnError(),
            ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>?> VerifyFormatting(CancellationToken ct = default)
    {
        // `fmt -check -list` names the files rather than only failing, which is the difference
        // between a report a reader can act on and one that sends them back to a terminal.
        var result = await commands.Run(Tofu("fmt", "-recursive", "-check", "-list=true", "-no-color").QuietOutput(), ct);
        if (result.IsSuccess)
        {
            return null;
        }

        var files = result.StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // An exit code with no listing is a failure to read the files at all, not a formatting one.
        return files.Length > 0 ? files : throw new CommandFailedException("tofu fmt could not read the root module.", result);
    }

    // Absolute, because -chdir moves where a relative path would resolve from.
    private static string[] VarFile(TofuEnvironment? environment) =>
        environment?.VarFile is { } file ? [$"-var-file={file.AbsolutePath}"] : [];

    // The env files are read as each command starts and travel on the command, not the process,
    // so a value lives exactly as long as the call that needs it.
    private async Task<Command> Tofu(TofuEnvironment? environment, CancellationToken ct, params string[] arguments)
    {
        var command = Tofu(arguments);
        if (environment is not { EnvFiles.Count: > 0 })
        {
            return command;
        }

        var variables = await EnvironmentFile.Load(environment.EnvFiles, secretProvider, ct);
        return variables.IsError
            ? throw new InvalidOperationException(string.Join(' ', variables.Errors.Select(e => e.Message)))
            : command.WithEnvironmentVariables(variables.Value);
    }

    // -chdir comes before the subcommand, which is the one place OpenTofu cares about order.
    private Command Tofu(params string[] arguments) =>
        Command.Create("tofu").WithArguments(options.Value.Root is { Length: > 0 } root
            ? [$"-chdir={root}", .. arguments]
            : arguments);
}
