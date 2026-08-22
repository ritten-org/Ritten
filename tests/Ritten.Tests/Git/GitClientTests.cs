using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.Git;
using Ritten.Reporting;

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
        await _git.CreateTag("v1.0.0", cancellationToken: TestContext.Current.CancellationToken);

        (await _git.TagExists("v1.0.0", TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await _git.RemoteTagExists("origin", "v1.0.0", TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task PushTag_MakesTheTagVisibleOnTheRemote()
    {
        await _git.CreateTag("v1.1.0", cancellationToken: TestContext.Current.CancellationToken);
        await _git.PushTag("origin", "v1.1.0", TestContext.Current.CancellationToken);

        (await _git.RemoteTagExists("origin", "v1.1.0", TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    private Task Git(params string[] arguments) =>
        _commands.Run(Command.Create("git").WithArguments(arguments).ThrowOnError(), TestContext.Current.CancellationToken);
}
