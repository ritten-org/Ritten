using Microsoft.Extensions.Logging.Abstractions;
using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.Engine.FileSystem;
using Ritten.Git;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Git;

/// <summary>
/// Integration tests against a real temporary git repository, with a bare sibling repository
/// acting as the <c>origin</c> remote.
/// </summary>
public class GitClientTests : IAsyncLifetime
{
    private readonly string _repository = Directory.CreateTempSubdirectory("ritten-git-").FullName;
    private readonly string _remote = Directory.CreateTempSubdirectory("ritten-git-remote-").FullName;
    private ICommandRunner _commands = null!;
    private GitClient _git = null!;

    public async ValueTask InitializeAsync()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.ProjectRoot.AbsolutePath.Returns(_repository);
        _commands = new CommandRunner(Substitute.For<IWorkflowLog>(), NullLogger<CommandRunner>.Instance, fileSystem);
        _git = new GitClient(_commands);

        await Git("init", "--initial-branch=main", ".");

        // A signing agent would stop to ask partway through a test.
        await Git("config", "commit.gpgsign", "false");
        await Git("-c", "user.name=Tests", "-c", "user.email=tests@example.com", "-c", "commit.gpgsign=false", "commit", "--allow-empty", "-m", "init");
        await Git("init", "--bare", _remote);
        await Git("remote", "add", "origin", _remote);
    }

    /// <summary>
    /// The repository the tests set up.
    /// </summary>
    private IDirectory Repository => new PhysicalDirectory(_repository);

    public ValueTask DisposeAsync()
    {
        Directory.Delete(_repository, recursive: true);
        Directory.Delete(_remote, recursive: true);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task RepositoryRoot_IsTheRepositoryNotTheWorkingDirectory()
    {
        // The command runs in a subdirectory, so this proves git answered rather than the path.
        var nested = Directory.CreateDirectory(Path.Combine(_repository, "src", "Thing"));
        var at = new PhysicalDirectory(nested.FullName);

        var root = await _git.RepositoryRoot(at, TestContext.Current.CancellationToken);

        // Asserted by what the directory is rather than by its path, which the platform is free
        // to resolve differently — macOS reaches the temp directory through a symlink.
        root.ShouldNotBeNull().Name.ShouldBe(Path.GetFileName(_repository));
        root.GetDirectory(".git").Exists.ShouldBeTrue();
    }

    [Fact]
    public async Task IsRepository_IsTrueInsideAWorkingTreeAndFalseOutside()
    {
        var nested = Directory.CreateDirectory(Path.Combine(_repository, "notes"));
        var outside = Directory.CreateTempSubdirectory("ritten-not-a-repo-");
        try
        {
            (await _git.IsRepository(Repository, TestContext.Current.CancellationToken)).ShouldBeTrue();
            (await _git.IsRepository(new PhysicalDirectory(nested.FullName), TestContext.Current.CancellationToken)).ShouldBeTrue();
            (await _git.IsRepository(new PhysicalDirectory(outside.FullName), TestContext.Current.CancellationToken)).ShouldBeFalse();
            // A bare repository is git's, but there is no working tree to commit in.
            (await _git.IsRepository(new PhysicalDirectory(_remote), TestContext.Current.CancellationToken)).ShouldBeFalse();
        }
        finally
        {
            outside.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task RepositoryRoot_IsNullOutsideARepository()
    {
        var outside = Directory.CreateTempSubdirectory("ritten-not-a-repo-");
        try
        {
            var at = new PhysicalDirectory(outside.FullName);

            (await _git.RepositoryRoot(at, TestContext.Current.CancellationToken)).ShouldBeNull();
        }
        finally
        {
            outside.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task GetRemoteUrl_ReturnsTheRemotesUrl()
    {
        var url = await _git.GetRemoteUrl(Repository, "origin", TestContext.Current.CancellationToken);

        url.ShouldBe(_remote);
    }

    [Fact]
    public async Task GetRemoteUrl_IsNullWhenTheRemoteDoesNotExist()
    {
        var url = await _git.GetRemoteUrl(Repository, "nowhere", TestContext.Current.CancellationToken);

        url.ShouldBeNull();
    }

    [Fact]
    public async Task Show_ReadsTheFileAsItExistsAtTheReference()
    {
        await File.WriteAllTextAsync(Path.Combine(_repository, "a.txt"), "committed", TestContext.Current.CancellationToken);
        await Git("add", "a.txt");
        await Git("-c", "user.name=Tests", "-c", "user.email=tests@example.com", "-c", "commit.gpgsign=false", "commit", "-m", "add a.txt");
        await File.WriteAllTextAsync(Path.Combine(_repository, "a.txt"), "changed", TestContext.Current.CancellationToken);

        var content = await _git.Show(Repository, "HEAD", "a.txt", TestContext.Current.CancellationToken);

        content.ShouldNotBeNull().Trim().ShouldBe("committed");
    }

    [Fact]
    public async Task Show_IsNullWhenTheFileDoesNotExistAtTheReference()
    {
        (await _git.Show(Repository, "HEAD", "missing.txt", TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task ChangedFiles_ReportsModifiedAndUntrackedFiles()
    {
        await File.WriteAllTextAsync(Path.Combine(_repository, "tracked.txt"), "committed", TestContext.Current.CancellationToken);
        await Git("add", "tracked.txt");
        await Git("-c", "user.name=Tests", "-c", "user.email=tests@example.com", "-c", "commit.gpgsign=false", "commit", "-m", "add tracked.txt");
        await File.WriteAllTextAsync(Path.Combine(_repository, "tracked.txt"), "changed", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_repository, "untracked.txt"), "new", TestContext.Current.CancellationToken);

        var changes = await _git.ChangedFiles(Repository, ".", TestContext.Current.CancellationToken);

        changes.ShouldBe(["tracked.txt", "untracked.txt"], ignoreOrder: true);
    }

    [Fact]
    public async Task ChangedFiles_IsEmptyForACleanPath()
    {
        (await _git.ChangedFiles(Repository, ".", TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task TagExists_IsFalseForAMissingTag()
    {
        (await _git.TagExists(Repository, "v9.9.9", TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task CreateTag_MakesTheTagVisibleLocally()
    {
        await _git.CreateTag(Repository, "v1.0.0", ct: TestContext.Current.CancellationToken);

        (await _git.TagExists(Repository, "v1.0.0", TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await _git.RemoteTagExists(Repository, "origin", "v1.0.0", TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task PushTag_MakesTheTagVisibleOnTheRemote()
    {
        await _git.CreateTag(Repository, "v1.1.0", ct: TestContext.Current.CancellationToken);
        await _git.PushTag(Repository, "origin", "v1.1.0", TestContext.Current.CancellationToken);

        (await _git.RemoteTagExists(Repository, "origin", "v1.1.0", TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    [Fact]
    public async Task EachCall_AddressesTheRepositoryItNames()
    {
        var other = Directory.CreateTempSubdirectory("ritten-git-other-");
        try
        {
            await Git("init", "--initial-branch=trunk", other.FullName);

            var at = new PhysicalDirectory(other.FullName);

            (await _git.CurrentBranch(at, TestContext.Current.CancellationToken)).ShouldBe("trunk");
            (await _git.GetRemoteUrl(at, "origin", TestContext.Current.CancellationToken)).ShouldBeNull();
            (await _git.CurrentBranch(Repository, TestContext.Current.CancellationToken)).ShouldBe("main");
        }
        finally
        {
            other.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Upstream_IsNullUntilTheBranchHasOne()
    {
        (await _git.Upstream(Repository, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task AddRemote_MakesTheRemoteAnswerToItsName()
    {
        await _git.AddRemote(Repository, "mirror", "https://example.com/mirror.git", TestContext.Current.CancellationToken);

        (await _git.GetRemoteUrl(Repository, "mirror", TestContext.Current.CancellationToken)).ShouldBe("https://example.com/mirror.git");
    }

    [Fact]
    public async Task Stage_Commit_Push_LandTheChangeOnTheRemote()
    {
        await Git("config", "user.name", "Tests");
        await Git("config", "user.email", "tests@example.com");
        await File.WriteAllTextAsync(Path.Combine(_repository, "note.md"), "hello", TestContext.Current.CancellationToken);

        await _git.Stage(Repository, ".", TestContext.Current.CancellationToken);
        await _git.Commit(Repository, "Add a note", TestContext.Current.CancellationToken);
        await _git.Push(Repository, "origin", "main", setUpstream: true, ct: TestContext.Current.CancellationToken);

        (await _git.ChangedFiles(Repository, ".", TestContext.Current.CancellationToken)).ShouldBeEmpty();
        (await _git.Upstream(Repository, TestContext.Current.CancellationToken)).ShouldBe("origin/main");
        (await _git.CommitsAhead(Repository, "origin/main", TestContext.Current.CancellationToken)).ShouldBe(0);
        (await _git.Show(new PhysicalDirectory(_remote), "main", "note.md", TestContext.Current.CancellationToken))
            .ShouldNotBeNull().Trim().ShouldBe("hello");
    }

    [Fact]
    public async Task CommitsAhead_CountsWhatTheRemoteLacks()
    {
        await Git("config", "user.name", "Tests");
        await Git("config", "user.email", "tests@example.com");
        await _git.Push(Repository, "origin", "main", setUpstream: true, ct: TestContext.Current.CancellationToken);
        await Git("commit", "--allow-empty", "-m", "one");
        await Git("commit", "--allow-empty", "-m", "two");

        (await _git.CommitsAhead(Repository, "origin/main", TestContext.Current.CancellationToken)).ShouldBe(2);
    }

    [Fact]
    public async Task Stage_TakesDeletionsAsWellAsAdditions()
    {
        await Git("config", "user.name", "Tests");
        await Git("config", "user.email", "tests@example.com");
        await File.WriteAllTextAsync(Path.Combine(_repository, "gone.md"), "soon", TestContext.Current.CancellationToken);
        await _git.Stage(Repository, ".", TestContext.Current.CancellationToken);
        await _git.Commit(Repository, "Add", TestContext.Current.CancellationToken);
        File.Delete(Path.Combine(_repository, "gone.md"));

        await _git.Stage(Repository, ".", TestContext.Current.CancellationToken);
        await _git.Commit(Repository, "Remove", TestContext.Current.CancellationToken);

        (await _git.ChangedFiles(Repository, ".", TestContext.Current.CancellationToken)).ShouldBeEmpty();
        (await _git.Show(Repository, "HEAD", "gone.md", TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task Push_WithACredential_HandsItToGitThroughTheEnvironmentOnly()
    {
        var commands = new FakeCommandRunner();
        var git = new GitClient(commands);

        await git.Push(Repository, "origin", "main", new GitCredential("tom", "s3cret"), ct: TestContext.Current.CancellationToken);

        var push = commands.Executed.ShouldHaveSingleItem();
        push.Arguments.ShouldNotContain(a => a.Contains("s3cret") || a.Contains("tom"));
        push.EnvironmentVariables["RITTEN_GIT_USERNAME"].ShouldBe("tom");
        push.EnvironmentVariables["RITTEN_GIT_PASSWORD"].ShouldBe("s3cret");
        push.EnvironmentVariables["GIT_CONFIG_KEY_1"].ShouldBe("credential.helper");
        push.EnvironmentVariables["GIT_CONFIG_VALUE_0"].ShouldBe("");
    }

    [Fact]
    public async Task Push_WithoutACredential_LeavesTheEnvironmentAlone()
    {
        var commands = new FakeCommandRunner();
        var git = new GitClient(commands);

        await git.Push(Repository, "origin", "main", ct: TestContext.Current.CancellationToken);

        commands.Executed.ShouldHaveSingleItem().EnvironmentVariables.ShouldBeEmpty();
    }

    [Fact]
    public async Task Tags_ListsTheTagsThePatternMatches()
    {
        await Git("tag", "v1.0.0");
        await Git("tag", "v1.0.1");
        await Git("tag", "nightly");

        var tags = await _git.Tags(Repository, "v*", TestContext.Current.CancellationToken);

        tags.ShouldBe(["v1.0.0", "v1.0.1"]);
    }

    [Fact]
    public async Task IsShallow_IsFalseForAWholeClone()
    {
        (await _git.IsShallow(Repository, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task IsShallow_IsTrueForAClonePartOfTheHistory()
    {
        await Git("-c", "user.name=Tests", "-c", "user.email=tests@example.com", "-c", "commit.gpgsign=false", "commit", "--allow-empty", "-m", "second");
        var clone = Directory.CreateTempSubdirectory("ritten-git-shallow-");
        try
        {
            // file://, not a path: git ignores --depth for a local clone it can hard-link.
            await Git("clone", "--depth", "1", $"file://{_repository}", clone.FullName);

            (await _git.IsShallow(new PhysicalDirectory(clone.FullName), TestContext.Current.CancellationToken)).ShouldBeTrue();
        }
        finally
        {
            clone.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task TrackedFiles_ListsWhatGitTracksBeneathTheDirectory_RelativeToIt()
    {
        var component = Directory.CreateDirectory(Path.Combine(_repository, "monitoring", "grafana"));
        await File.WriteAllTextAsync(Path.Combine(component.FullName, "compose.yaml"), "", TestContext.Current.CancellationToken);
        Directory.CreateDirectory(Path.Combine(component.FullName, "config"));
        await File.WriteAllTextAsync(Path.Combine(component.FullName, "config", "rules file.yaml"), "", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(component.FullName, "notes.md"), "", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(component.FullName, "scratch.yaml"), "", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_repository, "elsewhere.yaml"), "", TestContext.Current.CancellationToken);
        await Git("add", "monitoring/grafana/compose.yaml", "monitoring/grafana/config", "monitoring/grafana/notes.md", "elsewhere.yaml");
        var at = new PhysicalDirectory(component.FullName);

        var all = await _git.TrackedFiles(at, ct: TestContext.Current.CancellationToken);
        var yaml = await _git.TrackedFiles(at, ["*.yaml"], TestContext.Current.CancellationToken);

        // The untracked scratch.yaml is not listed, nor is what lies outside the directory.
        all.ShouldBe(["compose.yaml", "config/rules file.yaml", "notes.md"]);
        yaml.ShouldBe(["compose.yaml", "config/rules file.yaml"]);
    }

    private Task Git(params string[] arguments) =>
        _commands.Run(Command.Create("git").WithArguments(arguments).ThrowOnError(), TestContext.Current.CancellationToken);

    [Fact]
    public async Task ChangedFilesSince_ReportsWhatTheBranchTouched()
    {
        await Git("checkout", "-b", "feature");
        await File.WriteAllTextAsync(Path.Combine(_repository, "touched.txt"), "x", TestContext.Current.CancellationToken);
        await Git("add", "touched.txt");
        await Git("-c", "user.name=Tests", "-c", "user.email=tests@example.com", "-c", "commit.gpgsign=false", "commit", "-m", "touch");

        var changed = await _git.ChangedFilesSince(Repository, "main", ".", TestContext.Current.CancellationToken);

        changed.ShouldBe(["touched.txt"]);
    }

    [Fact]
    public async Task ChangedFilesSince_IgnoresWhatTheBaseDidMeanwhile()
    {
        await Git("checkout", "-b", "feature");
        await File.WriteAllTextAsync(Path.Combine(_repository, "mine.txt"), "x", TestContext.Current.CancellationToken);
        await Git("add", "mine.txt");
        await Git("-c", "user.name=Tests", "-c", "user.email=tests@example.com", "-c", "commit.gpgsign=false", "commit", "-m", "mine");

        await Git("checkout", "main");
        await File.WriteAllTextAsync(Path.Combine(_repository, "theirs.txt"), "x", TestContext.Current.CancellationToken);
        await Git("add", "theirs.txt");
        await Git("-c", "user.name=Tests", "-c", "user.email=tests@example.com", "-c", "commit.gpgsign=false", "commit", "-m", "theirs");
        await Git("checkout", "feature");

        // Two dots would call theirs.txt a change of this branch's; three dots ask the merge base.
        var changed = await _git.ChangedFilesSince(Repository, "main", ".", TestContext.Current.CancellationToken);

        changed.ShouldBe(["mine.txt"]);
    }

    [Fact]
    public async Task ChangedFilesSince_NarrowsToThePathAsked()
    {
        await Git("checkout", "-b", "feature");
        Directory.CreateDirectory(Path.Combine(_repository, "inside"));
        await File.WriteAllTextAsync(Path.Combine(_repository, "inside", "a.txt"), "x", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_repository, "outside.txt"), "x", TestContext.Current.CancellationToken);
        await Git("add", ".");
        await Git("-c", "user.name=Tests", "-c", "user.email=tests@example.com", "-c", "commit.gpgsign=false", "commit", "-m", "both");

        var changed = await _git.ChangedFilesSince(Repository, "main", "inside", TestContext.Current.CancellationToken);

        changed.ShouldBe(["inside/a.txt"]);
    }

    [Fact]
    public async Task ChangedFilesSince_RefusesAReferenceItCannotResolve() =>
        // Empty would mean "nothing changed", which is what makes a caller skip its work.
        await Should.ThrowAsync<Exception>(
            _git.ChangedFilesSince(Repository, "origin/never-fetched", ".", TestContext.Current.CancellationToken));

    [Fact]
    public async Task FetchMergeBase_LetsAShallowCheckoutCompareWithItsBase()
    {
        // The shape of a CI checkout: one commit of the branch under review, no base, and a base
        // that has moved on since the branch left it — with more history than the first round
        // fetches, so a clone that stays shallow proves the merge base was found by deepening.
        await CommitFile("base.txt");
        for (var i = 0; i < GitClient.MergeBaseDepths[0]; i++)
        {
            await Git("-c", "user.name=Tests", "-c", "user.email=tests@example.com", "-c", "commit.gpgsign=false", "commit", "--allow-empty", "-m", $"history {i}");
        }

        await Git("checkout", "-b", "feature");
        await CommitFile("mine.txt");
        await CommitFile("mine-too.txt");
        await Git("checkout", "main");
        await CommitFile("theirs.txt");
        await Git("push", "origin", "main", "feature");

        await WithClone(["--depth", "1", "--branch", "feature"], async clone =>
        {
            await _git.FetchMergeBase(clone, "origin", "main", TestContext.Current.CancellationToken);

            var changed = await _git.ChangedFilesSince(clone, "origin/main", ".", TestContext.Current.CancellationToken);
            changed.ShouldBe(["mine.txt", "mine-too.txt"], ignoreOrder: true);
            (await _git.IsShallow(clone, TestContext.Current.CancellationToken)).ShouldBeTrue();
        });
    }

    [Fact]
    public async Task FetchMergeBase_FindsABaseFurtherBackThanTheFirstRound()
    {
        await Git("checkout", "-b", "feature");
        for (var i = 0; i <= GitClient.MergeBaseDepths[0]; i++)
        {
            await Git("-c", "user.name=Tests", "-c", "user.email=tests@example.com", "-c", "commit.gpgsign=false", "commit", "--allow-empty", "-m", $"step {i}");
        }

        await CommitFile("mine.txt");
        await Git("push", "origin", "main", "feature");

        await WithClone(["--depth", "1", "--branch", "feature"], async clone =>
        {
            await _git.FetchMergeBase(clone, "origin", "main", TestContext.Current.CancellationToken);

            (await _git.ChangedFilesSince(clone, "origin/main", ".", TestContext.Current.CancellationToken)).ShouldBe(["mine.txt"]);
        });
    }

    [Fact]
    public async Task FetchMergeBase_BringsAWholeCloneUpToDateWithoutMakingItShallow()
    {
        await Git("push", "origin", "main");
        await WithClone([], async clone =>
        {
            // The base moves on after the clone was made; the fetch must see it.
            await CommitFile("later.txt");
            await Git("push", "origin", "main");

            await _git.FetchMergeBase(clone, "origin", "main", TestContext.Current.CancellationToken);

            (await _git.Show(clone, "origin/main", "later.txt", TestContext.Current.CancellationToken)).ShouldNotBeNull();
            (await _git.IsShallow(clone, TestContext.Current.CancellationToken)).ShouldBeFalse();
        });
    }

    private async Task CommitFile(string name)
    {
        await File.WriteAllTextAsync(Path.Combine(_repository, name), name, TestContext.Current.CancellationToken);
        await Git("add", name);
        await Git("-c", "user.name=Tests", "-c", "user.email=tests@example.com", "-c", "commit.gpgsign=false", "commit", "-m", name);
    }

    private async Task WithClone(string[] options, Func<IDirectory, Task> test)
    {
        var clone = Directory.CreateTempSubdirectory("ritten-git-clone-");
        try
        {
            // file://, not a path: git ignores --depth for a local clone it can hard-link.
            await Git(["clone", .. options, $"file://{_remote}", clone.FullName]);
            await test(new PhysicalDirectory(clone.FullName));
        }
        finally
        {
            clone.Delete(recursive: true);
        }
    }
}
