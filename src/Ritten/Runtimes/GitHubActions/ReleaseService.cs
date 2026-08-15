using Microsoft.Extensions.Options;
using Octokit;
using Ritten.Contracts;

namespace Ritten.Runtimes.GitHubActions;

internal class ReleaseService(IPipelineLog log, IOptions<GitHubOptions> options, IGitHubClient client) : IReleaseService
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

    public async Task Create(string tag, string title, string notes, CancellationToken cancellationToken = default)
    {
        await client.Repository.Release.Create(RepositoryId, new NewRelease(tag) { Name = title, Body = notes });
        log.Detail($"Created the GitHub release {title} for tag {tag}.");
    }

    private long RepositoryId => options.Value.RepositoryId
        ?? throw new InvalidOperationException($"The GitHub repository ID is not available; it comes from {GitHubEnvironment.RepositoryId}, which GitHub Actions sets automatically.");
}
