using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Changelogs;
using Wolfe.Hamelin.DotNet;
using Wolfe.Hamelin.GitHub;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Create GitHub Release")]
public class CreateRelease(
    ILogger<CreateRelease> logger,
    IOptions<ReleaseOptions> options,
    IPipelineContext context,
    IReleaseService releases,
    IChangelog changelogs
) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var project = context.State.Get<Project>() ?? throw new Exception("Project info not found in state.");

        if (project.Version.IsPrerelease)
        {
            logger.LogInformation("Skipping GitHub Release for prerelease version {Version}; tag has still been pushed.", project.Version);
            return;
        }

        var tag = $"{options.Value.TagPrefix}{project.Version}";

        // A failed deploy may have already created the release; rerunning should carry on, not crash.
        if (await releases.Exists(tag, cancellationToken))
        {
            logger.LogInformation("GitHub Release {Tag} already exists; skipping.", tag);
            return;
        }

        var entry = context.State.Get<ChangelogEntry>() ?? throw new Exception("Changelog entry not found in state.");

        logger.LogInformation("Creating GitHub Release {Tag}.", tag);
        await releases.Create(tag, tag, changelogs.RenderEntry(entry), cancellationToken);
    }
}
