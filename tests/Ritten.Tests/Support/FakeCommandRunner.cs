using Ritten.Commands;

namespace Ritten.Tests.Support;

/// <summary>
/// A scripted <see cref="ICommandRunner"/> that records every command it's asked to run.
/// Unmatched commands succeed with no output; matched ones return their scripted result,
/// honouring <see cref="Command.ThrowsOnError"/> like the real runner.
/// </summary>
public sealed class FakeCommandRunner : ICommandRunner
{
    private readonly List<(Func<Command, bool> Match, CommandResult Result)> _responses = [];

    public List<Command> Executed { get; } = [];

    public void Respond(Func<Command, bool> match, CommandResult result) => _responses.Add((match, result));

    public Task<CommandResult> Run(Command command, CancellationToken cancellationToken = default)
    {
        Executed.Add(command);
        var result = _responses.FirstOrDefault(r => r.Match(command)).Result ?? new CommandResult(0, "", "");
        if (command.ThrowsOnError && result.IsError)
        {
            throw new CommandFailedException($"Command '{command.Path}' exited with code {result.ExitCode}.", result);
        }

        return Task.FromResult(result);
    }
}
