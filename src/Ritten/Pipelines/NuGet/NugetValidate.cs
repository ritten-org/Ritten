using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Reporting;

namespace Ritten.Pipelines.NuGet;

/// <summary>
/// Judges the project's <see cref="ReleaseState"/>: a version that is the latest of its line
/// passes, published or not, while one its line has moved past fails with what to bump.
/// </summary>
/// <param name="options">The pipeline's NuGet options.</param>
/// <param name="report">The build report.</param>
[Step("nuget validate", StepKind.Validation)]
public class NugetValidate(IOptions<NuGetOptions> options, IBuildReport report)
{
    /// <summary>
    /// Judges the release state of the given project's version.
    /// </summary>
    /// <param name="project">The project being validated.</param>
    /// <param name="releaseState">The release state determined against the feed.</param>
    public StepResult Run(Project project, ReleaseState releaseState)
    {
        // Name the line only when it isn't the whole story; single-line projects stay unqualified.
        var line = releaseState.OnLatestLine ? "" : $" on the {options.Value.Lines.Label(project.Version)} line";

        if (!releaseState.LatestInLine)
        {
            if (releaseState.Published)
            {
                report.Section("Release")
                    .Failure($"Version **{project.Version}** is already published, and **{releaseState.LatestVersionInLine}** is newer{line}. Bump `<Version>` in the project file.");
                return StepResult.Failed($"Version {project.Version} is already published, and {releaseState.LatestVersionInLine} is newer{line}.");
            }

            report.Section("Release")
                .Failure($"Version **{project.Version}** must be higher than **{releaseState.LatestVersionInLine}**, the latest published version{line}. Bump `<Version>` in the project file.");
            return StepResult.Failed($"Project version {project.Version} must be higher than {releaseState.LatestVersionInLine}, the latest published version{line}.");
        }

        if (releaseState.Published)
        {
            report.Section("Release")
                .Success(releaseState.OnLatestLine
                    ? $"Version **{project.Version}** is the latest published version; nothing new to release."
                    : $"Version **{project.Version}** is the latest on the {options.Value.Lines.Label(project.Version)} line; nothing new to release (latest overall: **{releaseState.LatestVersion}**).");
            return StepResult.Successful;
        }

        report.Section("Release")
            .Success(releaseState.LatestVersion == null
                ? $"Version **{project.Version}** will be the first published version of {project.Name}."
                : project.Version < releaseState.LatestVersion
                    ? $"Version **{project.Version}** is a backport to the {options.Value.Lines.Label(project.Version)} line (latest overall: **{releaseState.LatestVersion}**)."
                    : $"Version **{project.Version}** is valid (latest published: **{releaseState.LatestVersion}**).");
        return StepResult.Successful;
    }
}
