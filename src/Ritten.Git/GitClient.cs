using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.Engine.FileSystem;

namespace Ritten.Git;

internal class GitClient(ICommandRunner commands) : IGit
{
    public async Task<IDirectory?> RepositoryRoot(CancellationToken cancellationToken = default)
    {
        var result = await commands.Run(
            Command.Create("git").WithArguments("rev-parse", "--show-toplevel").QuietOutput(),
            cancellationToken);
        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput)
            ? new PhysicalDirectory(result.StandardOutput.Trim())
            : null;
    }

    public async Task<string?> GetRemoteUrl(string remote, CancellationToken cancellationToken = default)
    {
        var result = await commands.Run(
            Command.Create("git").WithArguments("remote", "get-url", remote).QuietOutput(),
            cancellationToken);
        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput)
            ? result.StandardOutput.Trim()
            : null;
    }

    public async Task<string?> Show(string reference, string path, CancellationToken cancellationToken = default)
    {
        // A file that doesn't exist at the reference is an expected answer, not a failure.
        var result = await commands.Run(
            Command.Create("git").WithArguments("show", $"{reference}:{path}").QuietOutput(),
            cancellationToken);
        return result.IsSuccess ? result.StandardOutput : null;
    }

    public async Task<IReadOnlyList<string>> ChangedFiles(string path, CancellationToken cancellationToken = default)
    {
        // --porcelain rather than `diff --quiet` so that untracked files are reported too.
        var result = await commands.Run(
            Command.Create("git").WithArguments("status", "--porcelain", "--", path).QuietOutput().ThrowOnError(),
            cancellationToken);

        // Porcelain status codes are fixed-width ("XY <path>"), so the path starts at offset 3 —
        // trimming earlier would shift entries like " M path". Renames appear as "XY <old> -> <new>",
        // and the new path is the one that exists now.
        return
        [
            .. result.StandardOutput
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Length > 3 ? line[3..].Trim() : line.Trim())
                .Select(line => line.Contains("->", StringComparison.Ordinal)
                    ? line[(line.IndexOf("->", StringComparison.Ordinal) + 2)..].Trim()
                    : line)
                .Select(line => line.Trim('"'))
                .Where(line => !string.IsNullOrWhiteSpace(line))
        ];
    }

    public async Task<bool> TagExists(string tag, CancellationToken cancellationToken = default)
    {
        var result = await commands.Run(
            Command.Create("git").WithArguments("rev-parse", "--verify", "--quiet", $"refs/tags/{tag}").QuietOutput(),
            cancellationToken);
        return result.IsSuccess;
    }

    public async Task<bool> RemoteTagExists(string remote, string tag, CancellationToken cancellationToken = default)
    {
        var result = await commands.Run(
            Command.Create("git").WithArguments("ls-remote", "--tags", remote, $"refs/tags/{tag}").QuietOutput().ThrowOnError(),
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
