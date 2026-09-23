using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Releases;
using Ritten.Reporting;

namespace Ritten.NuGet.Steps;

/// <summary>
/// Judges the project's <see cref="ReleaseState"/>.
/// </summary>
/// <remarks>
/// Under <see cref="ReleaseCadence.Continuous"/> a published version is only at rest when nothing it ships has
/// changed: a pull request that changes what ships must move the version, because its merge is the release.
/// That needs <see cref="ShippedChanges"/> from <see cref="ReadShippedChanges"/> earlier in the job.
/// </remarks>
/// <param name="options">The workflow's NuGet options.</param>
/// <param name="report">The build report.</param>
[Step("check version", StepKind.Check)]
public class CheckVersion(IOptions<NuGetOptions> options, IWorkflowReport report)
{

    // TODO: Split this up. Something else should produce the versions (NPM, NuGet, etc.) this step should do the actual enforcement.

    /// <summary>
    /// Judges the release state of the given project's version.
    /// </summary>
    /// <param name="project">The project being validated.</param>
    /// <param name="releaseState">The release state determined against the feed.</param>
    /// <param name="shipped">What the pull request changed of what ships; required under a continuous cadence.</param>
    public StepResult Run(Project project, ReleaseState releaseState, ShippedChanges? shipped)
    {
        // Name the line only when it isn't the whole story; single-line projects stay unqualified.
        var line = releaseState.OnLatestLine ? "" : $" on the {options.Value.Lines.Label(project.Version)} line";

        if (!releaseState.LatestInLine)
        {
            if (releaseState.Published)
            {
                report.Section(SectionName.Version)
                    .Failure($"Version **{project.Version}** is already published, and **{releaseState.LatestVersionInLine}** is newer{line}. Bump `<Version>` in the project file.");
                return StepResult.Failed($"Version {project.Version} is already published, and {releaseState.LatestVersionInLine} is newer{line}.");
            }

            report.Section(SectionName.Version)
                .Failure($"Version **{project.Version}** must be higher than **{releaseState.LatestVersionInLine}**, the latest published version{line}. Bump `<Version>` in the project file.");
            return StepResult.Failed($"Project version {project.Version} must be higher than {releaseState.LatestVersionInLine}, the latest published version{line}.");
        }

        // Any package, not every: a version partly on the feed — a new package joining a lockstep release — would
        // otherwise ship the changed code under a number the other packages already hold with the old code.
        if (releaseState.AnyPublished && options.Value.Cadence == ReleaseCadence.Continuous)
        {
            if (shipped is null)
            {
                // A composition mistake rather than a finding: without the diff, "at rest" can't be told from
                // "changed and not bumped", and passing would be exactly the silence this cadence exists to prevent.
                return StepResult.Failed($"A continuous release cadence needs {nameof(ReadShippedChanges)} before {nameof(CheckVersion)}.");
            }

            if (shipped.Any)
            {
                report.Section(SectionName.Version)
                    .Failure($"Version **{project.Version}** is already published, but this pull request changes what it ships " +
                             $"({shipped.Files.Count} file(s) since `{shipped.BaseRef}`). Bump `<Version>` in the project file: " +
                             "under a continuous cadence the merge is the release.");
                return StepResult.Failed(
                    $"Version {project.Version} is already published, but {shipped.Files.Count} shipped file(s) changed since {shipped.BaseRef}; bump <Version>.");
            }
        }

        if (releaseState.Published)
        {
            report.Section(SectionName.Version)
                .Success(releaseState.OnLatestLine
                    ? $"Version **{project.Version}** is the latest published version; nothing new to release."
                    : $"Version **{project.Version}** is the latest on the {options.Value.Lines.Label(project.Version)} line; nothing new to release (latest overall: **{releaseState.LatestVersion}**).");
            return StepResult.Successful;
        }

        report.Section(SectionName.Version)
            .Success(releaseState.LatestVersion == null
                ? $"Version **{project.Version}** will be the first published version of {project.Name}."
                : project.Version < releaseState.LatestVersion
                    ? $"Version **{project.Version}** is a backport to the {options.Value.Lines.Label(project.Version)} line (latest overall: **{releaseState.LatestVersion}**)."
                    : $"Version **{project.Version}** is valid (latest published: **{releaseState.LatestVersion}**).");
        return StepResult.Successful;
    }
}
