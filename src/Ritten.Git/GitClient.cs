using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.Engine.FileSystem;

namespace Ritten.Git;

internal class GitClient : IGit
{
    private readonly ICommandRunner _commands;
    private readonly IDirectory? _repository;

    public GitClient(ICommandRunner commands) : this(commands, null)
    {
    }

    private GitClient(ICommandRunner commands, IDirectory? repository)
    {
        _commands = commands;
        _repository = repository;
    }

    public IGit InRepository(IDirectory repository) => new GitClient(_commands, repository);

    public async Task<bool> IsRepository(CancellationToken ct = default)
    {
        // Answers "true" inside a working tree and fails outside one (or inside a bare
        // repository, which has no working tree to commit in), so the exit code is the answer.
        var result = await _commands.Run(Git("rev-parse", "--is-inside-work-tree").QuietOutput(), ct);
        return result.IsSuccess && result.StandardOutput.Trim() == "true";
    }

    public async Task<IDirectory?> RepositoryRoot(CancellationToken ct = default)
    {
        var result = await _commands.Run(
            Git("rev-parse", "--show-toplevel").QuietOutput(),
            ct);
        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput)
            ? new PhysicalDirectory(result.StandardOutput.Trim())
            : null;
    }

    public async Task<string?> CurrentBranch(CancellationToken ct = default)
    {
        // Prints nothing rather than failing when HEAD is detached, so an empty answer is the null.
        var result = await _commands.Run(
            Git("branch", "--show-current").QuietOutput(),
            ct);
        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput)
            ? result.StandardOutput.Trim()
            : null;
    }

    public async Task<string?> Upstream(CancellationToken ct = default)
    {
        // A branch without an upstream is an expected answer, not a failure.
        var result = await _commands.Run(
            Git("rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{upstream}").QuietOutput(),
            ct);
        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput)
            ? result.StandardOutput.Trim()
            : null;
    }

    public async Task<int> CommitsAhead(string reference, CancellationToken ct = default)
    {
        var result = await _commands.Run(
            Git("rev-list", "--count", $"{reference}..HEAD").QuietOutput().ThrowOnError(),
            ct);
        return int.Parse(result.StandardOutput.Trim(), System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<string?> GetRemoteUrl(string remote, CancellationToken ct = default)
    {
        var result = await _commands.Run(
            Git("remote", "get-url", remote).QuietOutput(),
            ct);
        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput)
            ? result.StandardOutput.Trim()
            : null;
    }

    public async Task AddRemote(string remote, string url, CancellationToken ct = default) =>
        await _commands.Run(Git("remote", "add", remote, url).QuietOutput().ThrowOnError(), ct);

    public async Task<string?> Show(string reference, string path, CancellationToken ct = default)
    {
        // A file that doesn't exist at the reference is an expected answer, not a failure.
        var result = await _commands.Run(
            Git("show", $"{reference}:{path}").QuietOutput(),
            ct);
        return result.IsSuccess ? result.StandardOutput : null;
    }

    public async Task<IReadOnlyList<string>> ChangedFiles(string path, CancellationToken ct = default)
    {
        // --porcelain rather than `diff --quiet` so that untracked files are reported too.
        var result = await _commands.Run(
            Git("status", "--porcelain", "--", path).QuietOutput().ThrowOnError(),
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

    public async Task Stage(string path, CancellationToken ct = default) =>
        await _commands.Run(Git("add", "--all", "--", path).QuietOutput().ThrowOnError(), ct);

    public async Task Commit(string message, CancellationToken ct = default) =>
        await _commands.Run(Git("commit", "--quiet", "--message", message).ThrowOnError(), ct);

    public async Task Push(
        string remote,
        string branch,
        GitCredential? credential = null,
        bool setUpstream = false,
        CancellationToken ct = default)
    {
        var command = Git("push", "--quiet");
        if (setUpstream)
        {
            command = command.AndArguments("--set-upstream");
        }

        command = command.AndArguments(remote, branch);
        if (credential is not null)
        {
            command = command.WithEnvironmentVariables(CredentialEnvironment(credential));
        }

        await _commands.Run(command.ThrowOnError(), ct);
    }

    public async Task<bool> TagExists(string tag, CancellationToken ct = default)
    {
        var result = await _commands.Run(
            Git("rev-parse", "--verify", "--quiet", $"refs/tags/{tag}").QuietOutput(),
            ct);
        return result.IsSuccess;
    }

    public async Task<bool> RemoteTagExists(string remote, string tag, CancellationToken ct = default)
    {
        var result = await _commands.Run(
            Git("ls-remote", "--tags", remote, $"refs/tags/{tag}").QuietOutput().ThrowOnError(),
            ct);
        return !string.IsNullOrWhiteSpace(result.StandardOutput);
    }

    public async Task CreateTag(string tag, string? commit = null, CancellationToken ct = default)
    {
        var command = Git("tag", tag).ThrowOnError();
        if (!string.IsNullOrEmpty(commit))
        {
            command = command.AndArguments(commit);
        }

        await _commands.Run(command, ct);
    }

    public async Task PushTag(string remote, string tag, CancellationToken ct = default)
    {
        var command = Git("push", remote, tag).ThrowOnError();
        await _commands.Run(command, ct);
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
    private Command Git(params string[] arguments)
    {
        var command = Command.Create("git").WithArguments(arguments);
        return _repository is null ? command : command.InDirectory(_repository.AbsolutePath);
    }
}
