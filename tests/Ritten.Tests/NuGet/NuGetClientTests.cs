using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.NuGet;
using Ritten.Tests.Support;

namespace Ritten.Tests.NuGet;

public class NuGetClientTests
{
    private readonly FakeCommandRunner _commands = new();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IDirectory _cache = Substitute.For<IDirectory>();
    private readonly NuGetClient _client;

    public NuGetClientTests()
    {
        _cache.AbsolutePath.Returns("/tmp/ritten-nuget-cache-1");
        _fileSystem.CreateTempDirectory(Arg.Any<string>()).Returns(_cache);
        _client = new NuGetClient(_commands, _fileSystem);
    }

    // What `dotnet package search --exact-match --format json` writes: one entry for each version.
    private static string Search(params (string Id, string Version)[] packages) =>
        $$"""
        { "version": 2, "problems": [], "searchResult": [ { "sourceName": "feed", "packages": [ {{string.Join(", ", packages.Select(p => $$"""{ "id": "{{p.Id}}", "version": "{{p.Version}}" }"""))}} ] } ] }
        """;

    [Fact]
    public async Task Push_ComposesARedactedIdempotentCommand()
    {
        var package = Substitute.For<IFile>();
        package.AbsolutePath.Returns("/repo/artifacts/My.Package.1.0.0.nupkg");
        var feed = new NuGetFeed("https://feed.example/index.json").WithApiKey("secret-key");

        await _client.Push(feed, package, TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.ShouldBe([
            "nuget", "push", "/repo/artifacts/My.Package.1.0.0.nupkg",
            "--source", "https://feed.example/index.json",
            "--skip-duplicate",
            "--api-key", "secret-key"
        ]);
        command.ArgumentsRedacted.ShouldBeTrue();
        command.ThrowsOnError.ShouldBeTrue();
    }

    [Fact]
    public async Task Push_OmitsTheApiKeyWhenTheFeedHasNone()
    {
        var package = Substitute.For<IFile>();
        package.AbsolutePath.Returns("/repo/artifacts/My.Package.1.0.0.nupkg");

        await _client.Push(new NuGetFeed("https://feed.example/index.json"), package, TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.ShouldNotContain("--api-key");
        command.ArgumentsRedacted.ShouldBeFalse();
    }

    [Fact]
    public async Task GetPublishedVersions_AsksTheCliAsAReadWithACacheOfItsOwn()
    {
        _commands.Respond(_ => true, new CommandResult(0, Search(), ""));

        await _client.GetPublishedVersions(new NuGetFeed("https://feed.example/index.json"), "Test.Package", TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.ShouldBe([
            "package", "search", "Test.Package", "--exact-match", "--prerelease", "--format", "json", "--source", "https://feed.example/index.json"
        ]);
        command.EnvironmentVariables[EnvironmentVariableName.From("NUGET_HTTP_CACHE_PATH")].ShouldBe("/tmp/ritten-nuget-cache-1");
        command.Effect.ShouldBe(CommandEffect.None);
        command.ThrowsOnError.ShouldBeTrue();
        _cache.Received().Delete();
    }

    [Fact]
    public async Task GetPublishedVersions_ReturnsTheExactPackagesVersionsInAscendingOrder()
    {
        _commands.Respond(_ => true, new CommandResult(0,
            Search(("Test.Package", "1.0.0"), ("Test.Package", "0.9.0"), ("test.package", "1.1.0-beta.1"), ("Other.Package", "5.0.0")), ""));

        var versions = await _client.GetPublishedVersions(new NuGetFeed("https://feed.example/index.json"), "Test.Package", TestContext.Current.CancellationToken);

        versions.Select(v => v.ToString()).ShouldBe(["0.9.0", "1.0.0", "1.1.0-beta.1"]);
    }

    [Fact]
    public async Task GetPublishedVersions_ReturnsEmptyForAnUnpublishedPackage()
    {
        _commands.Respond(_ => true, new CommandResult(0, Search(), ""));

        (await _client.GetPublishedVersions(new NuGetFeed("https://feed.example/index.json"), "Test.Package", TestContext.Current.CancellationToken))
            .ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPublishedVersions_FailsWhenTheFeedCannotBeRead()
    {
        _commands.Respond(_ => true, new CommandResult(1, "error: Response status code does not indicate success: 401 (Unauthorized).", ""));

        await Should.ThrowAsync<CommandFailedException>(() =>
            _client.GetPublishedVersions(new NuGetFeed("https://feed.example/index.json"), "Test.Package", TestContext.Current.CancellationToken));
        _cache.Received().Delete();
    }

    [Fact]
    public void Versions_RefusesAnAnswerWithProblems() =>
        Should.Throw<InvalidOperationException>(() => NuGetClient.Versions(
            """{ "version": 2, "problems": [ { "text": "unreachable" } ], "searchResult": [] }""", "Test.Package"))
            .Message.ShouldContain("unreachable");
}
