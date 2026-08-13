using System.Net;
using Microsoft.Extensions.Options;
using Octokit;
using Ritten.Runtimes.GitHubActions;

namespace Ritten.Tests.GitHub;

public class ReleaseServiceTests
{
    private readonly IGitHubClient _client = Substitute.For<IGitHubClient>();
    private readonly ReleaseService _service;

    public ReleaseServiceTests()
    {
        _service = new ReleaseService(Options.Create(new GitHubOptions { RepositoryId = 42 }), _client);
    }

    [Fact]
    public async Task Exists_IsTrueWhenTheReleaseIsFound()
    {
        (await _service.Exists("v1.0.0", TestContext.Current.CancellationToken)).ShouldBeTrue();

        await _client.Repository.Release.Received().Get(42, "v1.0.0");
    }

    [Fact]
    public async Task Exists_IsFalseWhenTheReleaseIsNotFound()
    {
        _client.Repository.Release.Get(42, "v1.0.0")
            .Returns<Release>(_ => throw new NotFoundException("missing", HttpStatusCode.NotFound));

        (await _service.Exists("v1.0.0", TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task Create_CreatesTheReleaseForTheTag()
    {
        await _service.Create("v1.0.0", "v1.0.0", "The notes.", TestContext.Current.CancellationToken);

        await _client.Repository.Release.Received().Create(42, Arg.Is<NewRelease>(r =>
            r.TagName == "v1.0.0" && r.Name == "v1.0.0" && r.Body == "The notes."));
    }

    [Fact]
    public async Task Throws_WhenTheRepositoryIdIsUnavailable()
    {
        var service = new ReleaseService(Options.Create(new GitHubOptions()), _client);

        await Should.ThrowAsync<InvalidOperationException>(() => service.Exists("v1.0.0", TestContext.Current.CancellationToken));
    }
}
