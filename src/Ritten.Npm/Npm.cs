using Ritten.Commands;

namespace Ritten.Npm;

/// <summary>
/// Runs <c>npm</c> in the project's directory.
/// </summary>
/// <param name="commands">The command runner.</param>
internal sealed class Npm(ICommandRunner commands) : INpm
{
    /// <inheritdoc />
    public Task<CommandResult> RunCommand(Func<Command, Command> configure, CancellationToken ct = default) =>
        commands.Run(configure(Command.Create("npm")), ct);

    /// <inheritdoc />
    public async Task Install(NpmProject project, CancellationToken ct = default) =>
        // Nothing to report about the advisories and the funding while installing.
        await commands.Run(In(project, Command.Create("npm").WithArguments("ci", "--no-audit", "--no-fund").WithEffect(CommandEffect.None)).ThrowOnError(), ct);

    /// <inheritdoc />
    public async Task<NpmScriptResult> Run(NpmProject project, string script, CancellationToken ct = default)
    {
        var result = await commands.Run(In(project, Command.Create("npm").WithArguments("run", script).WithEffect(CommandEffect.None)), ct);
        return new NpmScriptResult(!result.IsError, string.Join('\n', new[] { result.StandardOutput, result.StandardError }.Where(text => text.Length > 0)));
    }

    private static Command In(NpmProject project, Command command) =>
        project.Directory is { Length: > 0 } directory ? command.InDirectory(directory) : command;
}
