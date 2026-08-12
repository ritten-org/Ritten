using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.DotNet;
using Wolfe.Hamelin.NuGet;
using Wolfe.Hamelin.Reporting;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Publish NuGet Package")]
public class Publish(
    IOptions<NuGetOptions> options,
    IPipelineContext context,
    INuGet nuget,
    IBuildReport report
) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(options.Value.ApiKey))
        {
            throw new Exception("The NuGet API key is not configured; set NuGet__ApiKey for the deploy pipeline.");
        }

        var packed = context.State.Get<PackResult>() ?? throw new Exception("Pack result not found in state.");
        var feed = new NuGetFeed(options.Value.Feed).WithApiKey(options.Value.ApiKey);

        foreach (var package in packed.Packages)
        {
            await nuget.Push(feed, package, cancellationToken);
        }

        if (context.State.Get<Project>() is { } project)
        {
            report.Section("Release").Success($"Published **{project.Name} {project.Version}** to NuGet.");
        }
    }
}
