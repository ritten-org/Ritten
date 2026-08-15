using System.Net;
using Octokit;
using Ritten.Contracts;
using Ritten.GitHub;

namespace Ritten.Tests.GitHub;

public class ReleaseServiceTests
{
    private static readonly RepositoryPath Repository = new("example", "repo");

    private readonly IGitHubClient _client = Substitute.For<IGitHubClient>();
    private readonly ReleaseService _service;

    public ReleaseServiceTests()
    {
        _service = new ReleaseService(Substitute.For<IPipelineLog>(), _client);
    }

    [Fact]
    public async Task Exists_IsTrueWhenTheReleaseIsFound()
    {
        (await _service.Exists(Repository, "v1.0.0", TestContext.Current.CancellationToken)).ShouldBeTrue();

        await _client.Repository.Release.Received().Get("example", "repo", "v1.0.0");
    }

    [Fact]
    public async Task Exists_IsFalseWhenTheReleaseIsNotFound()
    {
        _client.Repository.Release.Get("example", "repo", "v1.0.0")
            .Returns<Release>(_ => throw new NotFoundException("missing", HttpStatusCode.NotFound));

        (await _service.Exists(Repository, "v1.0.0", TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task Create_CreatesTheReleaseForTheTag()
    {
        await _service.Create(Repository, "v1.0.0", "v1.0.0", "The notes.", cancellationToken: TestContext.Current.CancellationToken);

        await _client.Repository.Release.Received().Create("example", "repo", Arg.Is<NewRelease>(r =>
            r.TagName == "v1.0.0" && r.Name == "v1.0.0" && r.Body == "The notes." && r.MakeLatest == MakeLatestQualifier.True));
    }

    [Fact]
    public async Task Create_LeavesABackportUnmarkedAsLatest()
    {
        await _service.Create(Repository, "v1.0.0", "v1.0.0", "The notes.", makeLatest: false, TestContext.Current.CancellationToken);

        await _client.Repository.Release.Received().Create("example", "repo", Arg.Is<NewRelease>(r =>
            r.MakeLatest == MakeLatestQualifier.False));
    }
}
