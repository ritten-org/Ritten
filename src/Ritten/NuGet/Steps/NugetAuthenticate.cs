using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Reporting;

namespace Ritten.NuGet.Steps;

/// <summary>
/// Resolves the credentials for the feed being pushed to.
/// </summary>
/// <param name="job">The job being run.</param>
/// <param name="log">The workflow log.</param>
/// <param name="options">The workflow's NuGet options.</param>
/// <param name="prompt">The prompt used to ask for a key.</param>
[Step("nuget auth", StepKind.Work)]
public class NugetAuthenticate(WorkflowJob job, IWorkflowLog log, IOptions<NuGetOptions> options, IWorkflowPrompt prompt)
{
    /// <summary>
    /// Produces the authenticated feed that <see cref="NugetPush"/> publishes to.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult<NuGetFeed>> Run(CancellationToken cancellationToken = default)
    {
        var feed = new NuGetFeed(options.Value.Feed);

        if (options.Value.ApiKey is { } configured)
        {
            log.Detail($"Using the NuGet API key from {RittenEnvironment.NuGetApiKey}.");
            return feed.WithApiKey(configured);
        }

        if (job.DryRun)
        {
            log.Skipped("No API key needed: this is a dry run.");
            return feed;
        }

        if (!prompt.IsInteractive)
        {
            // Hanging on a build agent waiting for a person is worse than refusing to start.
            return StepResult.Failed($"Pushing to {options.Value.Feed} needs an API key, and there's no terminal to ask at. Set {RittenEnvironment.NuGetApiKey}.");
        }

        if (await prompt.Secret($"Enter the NuGet API key for {options.Value.Feed}:", cancellationToken) is not { } key)
        {
            return StepResult.Failed("No API key was provided.");
        }

        return feed.WithApiKey(key);
    }
}
