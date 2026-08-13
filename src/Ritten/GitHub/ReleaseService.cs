using Microsoft.Extensions.Options;
using Octokit;

namespace Ritten.GitHub;

internal class ReleaseService(IOptions<GitHubOptions> options, IGitHubClient client) : IReleaseService
{
    public async Task<bool> Exists(string tag, CancellationToken cancellationToken = default)
    {
        try
        {
            await client.Repository.Release.Get(RepositoryId, tag);
            return true;
        }
        catch (NotFoundException)
        {
            return false;
        }
    }

    public Task Create(string tag, string title, string notes, CancellationToken cancellationToken = default) =>
        client.Repository.Release.Create(RepositoryId, new NewRelease(tag) { Name = title, Body = notes });

    private long RepositoryId => options.Value.RepositoryId
        ?? throw new InvalidOperationException("The GitHub repository ID is not available; set GITHUB_REPOSITORY_ID or GitHub__RepositoryId.");
}
