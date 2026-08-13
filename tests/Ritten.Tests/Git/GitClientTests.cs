using Microsoft.Extensions.Logging.Abstractions;
using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.Git;

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
        fileSystem.CurrentDirectory.AbsolutePath.Returns(_repository);
        _commands = new CommandRunner(NullLogger<CommandRunner>.Instance, fileSystem);
        _git = new GitClient(_commands);

        await Git("init", "--initial-branch=main", ".");
        await Git("-c", "user.name=Tests", "-c", "user.email=tests@example.com", "commit", "--allow-empty", "-m", "init");
        await Git("init", "--bare", _remote);
        await Git("remote", "add", "origin", _remote);
    }

    public ValueTask DisposeAsync()
    {
        Directory.Delete(_repository, recursive: true);
        Directory.Delete(_remote, recursive: true);
        return ValueTask.CompletedTask;
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
