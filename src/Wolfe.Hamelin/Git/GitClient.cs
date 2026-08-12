using Wolfe.Hamelin.Commands;

namespace Wolfe.Hamelin.Git;

internal class GitClient(ICommandRunner commands) : IGit
{
    public async Task<bool> TagExists(string tag, CancellationToken cancellationToken = default)
    {
        var result = await commands.Run(
            Command.Create("git").WithArguments("rev-parse", "--verify", "--quiet", $"refs/tags/{tag}"),
            cancellationToken);
        return result.IsSuccess;
    }

    public async Task<bool> RemoteTagExists(string remote, string tag, CancellationToken cancellationToken = default)
    {
        var result = await commands.Run(
            Command.Create("git").WithArguments("ls-remote", "--tags", remote, $"refs/tags/{tag}").ThrowOnError(),
            cancellationToken);
        return !string.IsNullOrWhiteSpace(result.StandardOutput);
    }

    public async Task CreateTag(string tag, string? commit = null, CancellationToken cancellationToken = default)
    {
        var command = Command.Create("git").WithArguments("tag", tag).ThrowOnError();
        if (!string.IsNullOrEmpty(commit))
        {
            command = command.AndArguments(commit);
        }

        await commands.Run(command, cancellationToken);
    }

    public async Task PushTag(string remote, string tag, CancellationToken cancellationToken = default)
    {
        var command = Command.Create("git").WithArguments("push", remote, tag).ThrowOnError();
        await commands.Run(command, cancellationToken);
    }
}
