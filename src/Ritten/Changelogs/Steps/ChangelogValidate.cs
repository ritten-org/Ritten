using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Releases;
using Ritten.Reporting;

namespace Ritten.Changelogs.Steps;

/// <summary>
/// Validates that the current release is documented in the changelog.
/// </summary>
/// <param name="log">The pipeline log.</param>
/// <param name="report">The build report.</param>
[Step("changelog entry", StepKind.Validation)]
public class ChangelogValidate(IPipelineLog log, IBuildReport report)
{
    /// <summary>
    /// Validates the changelog for the given project and release state.
    /// </summary>
    /// <param name="project">The project being validated.</param>
    /// <param name="releaseState">The release state determined against the feed.</param>
    /// <param name="changelog">The parsed changelog (see <see cref="ReadChangelog"/>).</param>
    public StepResult Run(Project project, ReleaseState releaseState, Changelog changelog)
    {
        if (releaseState.Published)
        {
            report.Section("Release").Success("New changes accrue under **[Unreleased]** until a release is prepared.");
            log.Detail("This version is already published; no changelog entry required.");
            return StepResult.Successful;
        }

        // A prerelease ships whatever is in [Unreleased]; a release needs an entry of its own.
        // One or the other, never both — nothing writes a versioned heading before it ships.
        var entry = project.IsPrerelease ? changelog.Unreleased : changelog.Entry(project.Version);
        if (entry == null)
        {
            report.Section("Release").Failure(project.IsPrerelease
                ? "Missing [Unreleased] changelog entry."
                : $"Missing changelog entry for **{project.Version}**.");

            return StepResult.Failed(project.IsPrerelease
                ? "No [Unreleased] entry found in changelog."
                : $"No entry for version {project.Version} found in changelog.");
        }

        if (entry.IsEmpty)
        {
            report.Section("Release").Failure($"The changelog entry for **{project.Version}** is empty.");
            return StepResult.Failed($"Changelog entry for version {project.Version} is empty.");
        }

        report.Section("Release").Success($"Changelog entry for **{project.Version}** is present.");
        log.Detail($"Found changelog entry for {project.Version}.");
        return StepResult.Successful;
    }
}
