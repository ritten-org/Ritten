using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.Engine.FileSystem;
using Ritten.Git;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Git;

// What a dry run's command runner lets the git client do: read the repository, and fetch what a read needs, but
// stage, commit, tag, push and add remotes in words only.
public class GitDryRunTests
{
    private readonly FakeCommandRunner _commands = new();
    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();
    private readonly GitClient _git;

    public GitDryRunTests() => _git = new GitClient(new DryRunCommandRunner(_log, _commands));

    private static IDirectory Repository => new PhysicalDirectory("/repo");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ItReads()
    {
        await _git.CurrentBranch(Repository, Ct);
        await _git.TagExists(Repository, "v1.0.0", Ct);
        await _git.TrackedFiles(Repository, ct: Ct);

        _commands.Executed.Select(command => command.Arguments[0]).ShouldBe(["branch", "rev-parse", "ls-files"]);
    }

    [Fact]
    public async Task ItChangesNothing_AndSaysWhatItWouldHaveDone()
    {
        await _git.AddRemote(Repository, "origin", "https://code.twolfe.dev/tom-wolfe/Wolfe.Lab.git", Ct);
        await _git.Stage(Repository, "src", Ct);
        await _git.Commit(Repository, "release 1.0.0", Ct);
        await _git.Push(Repository, "origin", "main", new GitCredential("tom", "s3cret"), ct: Ct);
        await _git.CreateTag(Repository, "v1.0.0", "abc123", Ct);
        await _git.PushTag(Repository, "origin", "v1.0.0", Ct);

        _commands.Executed.ShouldBeEmpty();
        string[] said =
        [
            "Would add remote origin at https://code.twolfe.dev/tom-wolfe/Wolfe.Lab.git.",
            "Would stage the changes under src.",
            "Would commit \"release 1.0.0\".",
            "Would push main to origin as tom.",
            "Would create tag v1.0.0 at abc123.",
            "Would push tag v1.0.0 to origin."
        ];
        foreach (var message in said)
        {
            _log.Received().Log(WorkflowLogLevel.Skipped, message);
        }
    }
}
