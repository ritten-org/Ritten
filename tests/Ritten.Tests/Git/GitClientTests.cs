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
        _commands = new CommandRunner(Substitute.For<IWorkflowLog>(), fileSystem);
        _git = new GitClient(_commands);

        await Git("init", "--initial-branch=main", ".");
        await Git("-c", "user.name=Tests", "-c", "user.email=tests@example.com", "commit", "--allow-empty", "-m", "init");
        await Git("init", "--bare", _remote);
        await Git("remote", "add", "origin", _remote);
    }

    private ICommandRunner RunnerIn(string directory)
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.ProjectRoot.AbsolutePath.Returns(directory);
        return new CommandRunner(Substitute.For<IWorkflowLog>(), fileSystem);
    }

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
        var git = new GitClient(RunnerIn(nested.FullName));

        var root = await git.RepositoryRoot(TestContext.Current.CancellationToken);

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
            (await _git.IsRepository(TestContext.Current.CancellationToken)).ShouldBeTrue();
            (await _git.InRepository(new PhysicalDirectory(nested.FullName)).IsRepository(TestContext.Current.CancellationToken)).ShouldBeTrue();
            (await _git.InRepository(new PhysicalDirectory(outside.FullName)).IsRepository(TestContext.Current.CancellationToken)).ShouldBeFalse();
            // A bare repository is git's, but there is no working tree to commit in.
            (await _git.InRepository(new PhysicalDirectory(_remote)).IsRepository(TestContext.Current.CancellationToken)).ShouldBeFalse();
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
            var git = new GitClient(RunnerIn(outside.FullName));

            (await git.RepositoryRoot(TestContext.Current.CancellationToken)).ShouldBeNull();
        }
        finally
        {
            outside.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task GetRemoteUrl_ReturnsTheRemotesUrl()
    {
        var url = await _git.GetRemoteUrl("origin", TestContext.Current.CancellationToken);

        url.ShouldBe(_remote);
    }

    [Fact]
    public async Task GetRemoteUrl_IsNullWhenTheRemoteDoesNotExist()
    {
        var url = await _git.GetRemoteUrl("nowhere", TestContext.Current.CancellationToken);

        url.ShouldBeNull();
    }

    [Fact]
    public async Task Show_ReadsTheFileAsItExistsAtTheReference()
    {
        await File.WriteAllTextAsync(Path.Combine(_repository, "a.txt"), "committed", TestContext.Current.CancellationToken);
        await Git("add", "a.txt");
        await Git("-c", "user.name=Tests", "-c", "user.email=tests@example.com", "commit", "-m", "add a.txt");
        await File.WriteAllTextAsync(Path.Combine(_repository, "a.txt"), "changed", TestContext.Current.CancellationToken);

        var content = await _git.Show("HEAD", "a.txt", TestContext.Current.CancellationToken);

        content.ShouldNotBeNull().Trim().ShouldBe("committed");
    }

    [Fact]
    public async Task Show_IsNullWhenTheFileDoesNotExistAtTheReference()
    {
        (await _git.Show("HEAD", "missing.txt", TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task ChangedFiles_ReportsModifiedAndUntrackedFiles()
    {
        await File.WriteAllTextAsync(Path.Combine(_repository, "tracked.txt"), "committed", TestContext.Current.CancellationToken);
        await Git("add", "tracked.txt");
        await Git("-c", "user.name=Tests", "-c", "user.email=tests@example.com", "commit", "-m", "add tracked.txt");
        await File.WriteAllTextAsync(Path.Combine(_repository, "tracked.txt"), "changed", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_repository, "untracked.txt"), "new", TestContext.Current.CancellationToken);

        var changes = await _git.ChangedFiles(".", TestContext.Current.CancellationToken);

        changes.ShouldBe(["tracked.txt", "untracked.txt"], ignoreOrder: true);
    }

    [Fact]
    public async Task ChangedFiles_IsEmptyForACleanPath()
    {
        (await _git.ChangedFiles(".", TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task TagExists_IsFalseForAMissingTag()
    {
        (await _git.TagExists("v9.9.9", TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task CreateTag_MakesTheTagVisibleLocally()
    {
        await _git.CreateTag("v1.0.0", ct: TestContext.Current.CancellationToken);

        (await _git.TagExists("v1.0.0", TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await _git.RemoteTagExists("origin", "v1.0.0", TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task PushTag_MakesTheTagVisibleOnTheRemote()
    {
        await _git.CreateTag("v1.1.0", ct: TestContext.Current.CancellationToken);
        await _git.PushTag("origin", "v1.1.0", TestContext.Current.CancellationToken);

        (await _git.RemoteTagExists("origin", "v1.1.0", TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    [Fact]
    public async Task InRepository_AddressesThatRepositoryInsteadOfTheWorkingDirectory()
    {
        var other = Directory.CreateTempSubdirectory("ritten-git-other-");
        try
        {
            await Git("init", "--initial-branch=trunk", other.FullName);

            var git = _git.InRepository(new PhysicalDirectory(other.FullName));

            (await git.CurrentBranch(TestContext.Current.CancellationToken)).ShouldBe("trunk");
            (await git.GetRemoteUrl("origin", TestContext.Current.CancellationToken)).ShouldBeNull();
            (await _git.CurrentBranch(TestContext.Current.CancellationToken)).ShouldBe("main");
        }
        finally
        {
            other.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Upstream_IsNullUntilTheBranchHasOne()
    {
        (await _git.Upstream(TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task AddRemote_MakesTheRemoteAnswerToItsName()
    {
        await _git.AddRemote("mirror", "https://example.com/mirror.git", TestContext.Current.CancellationToken);

        (await _git.GetRemoteUrl("mirror", TestContext.Current.CancellationToken)).ShouldBe("https://example.com/mirror.git");
    }

    [Fact]
    public async Task Stage_Commit_Push_LandTheChangeOnTheRemote()
    {
        await Git("config", "user.name", "Tests");
        await Git("config", "user.email", "tests@example.com");
        await File.WriteAllTextAsync(Path.Combine(_repository, "note.md"), "hello", TestContext.Current.CancellationToken);

        await _git.Stage(".", TestContext.Current.CancellationToken);
        await _git.Commit("Add a note", TestContext.Current.CancellationToken);
        await _git.Push("origin", "main", setUpstream: true, ct: TestContext.Current.CancellationToken);

        (await _git.ChangedFiles(".", TestContext.Current.CancellationToken)).ShouldBeEmpty();
        (await _git.Upstream(TestContext.Current.CancellationToken)).ShouldBe("origin/main");
        (await _git.CommitsAhead("origin/main", TestContext.Current.CancellationToken)).ShouldBe(0);
        (await _git.InRepository(new PhysicalDirectory(_remote)).Show("main", "note.md", TestContext.Current.CancellationToken))
            .ShouldNotBeNull().Trim().ShouldBe("hello");
    }

    [Fact]
    public async Task CommitsAhead_CountsWhatTheRemoteLacks()
    {
        await Git("config", "user.name", "Tests");
        await Git("config", "user.email", "tests@example.com");
        await _git.Push("origin", "main", setUpstream: true, ct: TestContext.Current.CancellationToken);
        await Git("commit", "--allow-empty", "-m", "one");
        await Git("commit", "--allow-empty", "-m", "two");

        (await _git.CommitsAhead("origin/main", TestContext.Current.CancellationToken)).ShouldBe(2);
    }

    [Fact]
    public async Task Stage_TakesDeletionsAsWellAsAdditions()
    {
        await Git("config", "user.name", "Tests");
        await Git("config", "user.email", "tests@example.com");
        await File.WriteAllTextAsync(Path.Combine(_repository, "gone.md"), "soon", TestContext.Current.CancellationToken);
        await _git.Stage(".", TestContext.Current.CancellationToken);
        await _git.Commit("Add", TestContext.Current.CancellationToken);
        File.Delete(Path.Combine(_repository, "gone.md"));

        await _git.Stage(".", TestContext.Current.CancellationToken);
        await _git.Commit("Remove", TestContext.Current.CancellationToken);

        (await _git.ChangedFiles(".", TestContext.Current.CancellationToken)).ShouldBeEmpty();
        (await _git.Show("HEAD", "gone.md", TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task Push_WithACredential_HandsItToGitThroughTheEnvironmentOnly()
    {
        var commands = new FakeCommandRunner();
        var git = new GitClient(commands);

        await git.Push("origin", "main", new GitCredential("tom", "s3cret"), ct: TestContext.Current.CancellationToken);

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

        await git.Push("origin", "main", ct: TestContext.Current.CancellationToken);

        commands.Executed.ShouldHaveSingleItem().EnvironmentVariables.ShouldBeEmpty();
    }

    private Task Git(params string[] arguments) =>
        _commands.Run(Command.Create("git").WithArguments(arguments).ThrowOnError(), TestContext.Current.CancellationToken);
}
