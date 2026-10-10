using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.Engine.FileSystem;

namespace Ritten.Git;

internal class GitClient(ICommandRunner commands) : IGit
{
    public async Task<bool> IsRepository(IDirectory repository, CancellationToken ct = default)
    {
        // Answers "true" inside a working tree and fails outside one (or inside a bare
        // repository, which has no working tree to commit in), so the exit code is the answer.
        var result = await commands.Run(Git(repository, "rev-parse", "--is-inside-work-tree").QuietOutput(), ct);
        return result.IsSuccess && result.StandardOutput.Trim() == "true";
    }

    public async Task<IDirectory?> RepositoryRoot(IDirectory repository, CancellationToken ct = default)
    {
        var result = await commands.Run(
            Git(repository, "rev-parse", "--show-toplevel").QuietOutput(),
            ct);
        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput)
            ? new PhysicalDirectory(result.StandardOutput.Trim())
            : null;
    }

    public async Task<string?> CurrentBranch(IDirectory repository, CancellationToken ct = default)
    {
        // Prints nothing rather than failing when HEAD is detached, so an empty answer is the null.
        var result = await commands.Run(
            Git(repository, "branch", "--show-current").QuietOutput(),
            ct);
        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput)
            ? result.StandardOutput.Trim()
            : null;
    }

    public async Task<string?> Upstream(IDirectory repository, CancellationToken ct = default)
    {
        // A branch without an upstream is an expected answer, not a failure.
        var result = await commands.Run(
            Git(repository, "rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{upstream}").QuietOutput(),
            ct);
        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput)
            ? result.StandardOutput.Trim()
            : null;
    }

    public async Task<int> CommitsAhead(IDirectory repository, string reference, CancellationToken ct = default)
    {
        var result = await commands.Run(
            Git(repository, "rev-list", "--count", $"{reference}..HEAD").QuietOutput().ThrowOnError(),
            ct);
        return int.Parse(result.StandardOutput.Trim(), System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<string?> GetRemoteUrl(IDirectory repository, string remote, CancellationToken ct = default)
    {
        var result = await commands.Run(
            Git(repository, "remote", "get-url", remote).QuietOutput(),
            ct);
        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput)
            ? result.StandardOutput.Trim()
            : null;
    }

    public async Task AddRemote(IDirectory repository, string remote, string url, CancellationToken ct = default) =>
        await commands.Run(Git(repository, "remote", "add", remote, url).QuietOutput().ThrowOnError(), ct);

    public async Task<string?> Show(IDirectory repository, string reference, string path, CancellationToken ct = default)
    {
        // A file that doesn't exist at the reference is an expected answer, not a failure.
        var result = await commands.Run(
            Git(repository, "show", $"{reference}:{path}").QuietOutput(),
            ct);
        return result.IsSuccess ? result.StandardOutput : null;
    }

    public async Task<IReadOnlyList<string>> ChangedFilesSince(IDirectory repository, string reference, string path, CancellationToken ct = default)
    {
        // Three dots, not two: the merge base rather than the reference's tip, so work the base
        // has done since the branch started is not reported as this branch's.
        var result = await commands.Run(
            Git(repository, "diff", "--name-only", $"{reference}...HEAD", "--", path).QuietOutput().ThrowOnError(),
            ct);

        return
        [
            .. result.StandardOutput
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim().Trim('"'))
                .Where(line => line.Length > 0)
        ];
    }

    public async Task FetchMergeBase(IDirectory repository, string remote, string branch, CancellationToken ct = default)
    {
        string[] refspec = [remote, $"+refs/heads/{branch}:refs/remotes/{remote}/{branch}"];
        if (!await IsShallow(repository, ct))
        {
            await commands.Run(Git(repository, ["fetch", "--quiet", "--no-tags", .. refspec]).QuietOutput().ThrowOnError(), ct);
            return;
        }

        // Fetching the base alone is not enough: HEAD's own history ends at the shallow boundary,
        // so the two never meet. Naming HEAD's commit deepens it alongside, and a few rounds find
        // the merge base of any ordinary pull request without fetching the whole history.
        var head = (await commands.Run(Git(repository, "rev-parse", "HEAD").QuietOutput().ThrowOnError(), ct)).StandardOutput.Trim();
        foreach (var depth in MergeBaseDepths)
        {
            // Not every server serves a commit by its id, so a refused fetch falls through to the last resort.
            var fetched = await commands.Run(Git(repository, ["fetch", "--quiet", "--no-tags", $"--depth={depth}", .. refspec, head]).QuietOutput(), ct);
            if (fetched.IsSuccess && await HasMergeBase(repository, $"refs/remotes/{remote}/{branch}", ct))
            {
                return;
            }
        }

        await commands.Run(Git(repository, ["fetch", "--quiet", "--no-tags", "--unshallow", .. refspec]).QuietOutput().ThrowOnError(), ct);
    }

    /// <summary>
    /// How deep <see cref="FetchMergeBase"/> looks before giving up and fetching everything.
    /// </summary>
    internal static readonly int[] MergeBaseDepths = [50, 1000];

    private async Task<bool> HasMergeBase(IDirectory repository, string reference, CancellationToken ct) =>
        (await commands.Run(Git(repository, "merge-base", reference, "HEAD").QuietOutput(), ct)).IsSuccess;

    public async Task<IReadOnlyList<string>> ChangedFiles(IDirectory repository, string path, CancellationToken ct = default)
    {
        // --porcelain rather than `diff --quiet` so that untracked files are reported too.
        var result = await commands.Run(
            Git(repository, "status", "--porcelain", "--", path).QuietOutput().ThrowOnError(),
            ct);

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

    public async Task Stage(IDirectory repository, string path, CancellationToken ct = default) =>
        await commands.Run(Git(repository, "add", "--all", "--", path).QuietOutput().ThrowOnError(), ct);

    public async Task Commit(IDirectory repository, string message, CancellationToken ct = default) =>
        await commands.Run(Git(repository, "commit", "--quiet", "--message", message).ThrowOnError(), ct);

    public async Task Push(
        IDirectory repository,
        string remote,
        string branch,
        GitCredential? credential = null,
        bool setUpstream = false,
        CancellationToken ct = default)
    {
        var command = Git(repository, "push", "--quiet");
        if (setUpstream)
        {
            command = command.AndArguments("--set-upstream");
        }

        command = command.AndArguments(remote, branch);
        if (credential is not null)
        {
            command = command.WithEnvironmentVariables(CredentialEnvironment(credential));
        }

        await commands.Run(command.ThrowOnError(), ct);
    }

    public async Task<bool> TagExists(IDirectory repository, string tag, CancellationToken ct = default)
    {
        var result = await commands.Run(
            Git(repository, "rev-parse", "--verify", "--quiet", $"refs/tags/{tag}").QuietOutput(),
            ct);
        return result.IsSuccess;
    }

    public async Task<bool> RemoteTagExists(IDirectory repository, string remote, string tag, CancellationToken ct = default)
    {
        var result = await commands.Run(
            Git(repository, "ls-remote", "--tags", remote, $"refs/tags/{tag}").QuietOutput().ThrowOnError(),
            ct);
        return !string.IsNullOrWhiteSpace(result.StandardOutput);
    }

    public async Task CreateTag(IDirectory repository, string tag, string? commit = null, CancellationToken ct = default)
    {
        var command = Git(repository, "tag", tag).ThrowOnError();
        if (!string.IsNullOrEmpty(commit))
        {
            command = command.AndArguments(commit);
        }

        await commands.Run(command, ct);
    }

    public async Task PushTag(IDirectory repository, string remote, string tag, CancellationToken ct = default)
    {
        var command = Git(repository, "push", remote, tag).ThrowOnError();
        await commands.Run(command, ct);
    }

    /// <summary>
    /// The credential reaches git through a helper declared in the environment for this one
    /// invocation: nothing is written to the repository's configuration or the remote URL, the
    /// values never appear in an argument list, and the empty first entry resets whatever helpers
    /// the machine has configured so a stored credential can't answer for the one given.
    /// </summary>
    internal static Dictionary<string, string> CredentialEnvironment(GitCredential credential) => new()
    {
        ["GIT_CONFIG_COUNT"] = "2",
        ["GIT_CONFIG_KEY_0"] = "credential.helper",
        ["GIT_CONFIG_VALUE_0"] = "",
        ["GIT_CONFIG_KEY_1"] = "credential.helper",
        ["GIT_CONFIG_VALUE_1"] = "!f() { echo \"username=$RITTEN_GIT_USERNAME\"; echo \"password=$RITTEN_GIT_PASSWORD\"; }; f",
        ["RITTEN_GIT_USERNAME"] = credential.Username,
        ["RITTEN_GIT_PASSWORD"] = credential.Password
    };

    /// <summary>
    /// A git command against the repository this client addresses: the working directory when
    /// none was named, since git finds the repository from wherever it runs.
    /// </summary>
    public async Task<IReadOnlyList<string>> Tags(IDirectory repository, string pattern = "*", CancellationToken ct = default)
    {
        var result = await commands.Run(Git(repository, "tag", "--list", pattern).QuietOutput().ThrowOnError(), ct);
        return result.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public async Task<bool> IsShallow(IDirectory repository, CancellationToken ct = default)
    {
        var result = await commands.Run(Git(repository, "rev-parse", "--is-shallow-repository").QuietOutput().ThrowOnError(), ct);
        return result.StandardOutput.Trim() == "true";
    }

    public async Task<IReadOnlyList<string>> TrackedFiles(IDirectory repository, IReadOnlyList<string>? pathspecs = null, CancellationToken ct = default)
    {
        string[] limits = pathspecs is { Count: > 0 } ? ["--", .. pathspecs] : [];
        var result = await commands.Run(Git(repository, ["ls-files", "-z", .. limits]).QuietOutput().ThrowOnError(), ct);

        // NUL-separated, so a name is read exactly as written; the runner ends what it captured with a newline.
        return result.StandardOutput.TrimEnd('\n').Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }

    private static Command Git(IDirectory repository, params string[] arguments) =>
        Command.Create("git").WithArguments(arguments).InDirectory(repository.AbsolutePath);
}
