using Octokit;
using Ritten.Reporting;

namespace Ritten.GitHub;

internal class ReleaseService(IWorkflowLog log, IGitHubClient client) : IReleaseService
{
    public async Task<bool> Exists(RepositoryPath repository, string tag, CancellationToken cancellationToken = default)
    {
        try
        {
            await client.Repository.Release.Get(repository.Owner, repository.Name, tag);
            return true;
        }
        catch (NotFoundException)
        {
            return false;
        }
    }

    public async Task Create(RepositoryPath repository, string tag, string title, string notes, bool makeLatest = true, CancellationToken cancellationToken = default)
    {
        await client.Repository.Release.Create(repository.Owner, repository.Name, new NewRelease(tag)
        {
            Name = title,
            Body = notes,
            MakeLatest = makeLatest ? MakeLatestQualifier.True : MakeLatestQualifier.False
        });
        log.Detail($"Created the GitHub release {title} for tag {tag} in {repository}.");
    }
}
