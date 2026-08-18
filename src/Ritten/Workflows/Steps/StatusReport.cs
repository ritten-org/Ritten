using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Releases;

namespace Ritten.Workflows.Steps;

/// <summary>
/// Says where the project stands: its version, its release state, and what the changelog holds.
/// Observation only — nothing here fails on policy; <c>check</c> is where judgment lives.
/// </summary>
/// <param name="log">The workflow log.</param>
[Step("status", StepKind.Work)]
public class StatusReport(IWorkflowLog log)
{
    /// <summary>
    /// Reports the project's standing.
    /// </summary>
    /// <param name="project">The project being described.</param>
    /// <param name="releaseState">The release state determined against the feed.</param>
    /// <param name="changelog">The parsed changelog.</param>
    public StepResult Run(Project project, ReleaseState releaseState, Changelog changelog)
    {
        log.Status(Standing(project, releaseState));
        log.Status(NextMove(project, releaseState, changelog));
        return StepResult.Successful;
    }

    private static string Standing(Project project, ReleaseState state) => (state.Published, state.LatestInLine) switch
    {
        (true, true) => state.OnLatestLine
            ? $"{project.Name} {project.Version} is published and at rest."
            : $"{project.Name} {project.Version} is published and at rest on its line (latest overall: {state.LatestVersion}).",
        (true, false) =>
            $"{project.Name} {project.Version} is published, but {state.LatestVersionInLine} is newer on its line — the version has been wound back.",
        (false, false) =>
            $"{project.Name} {project.Version} has been overtaken: its line has moved on to {state.LatestVersionInLine}.",
        _ => state.LatestVersion == null
            ? $"{project.Name} {project.Version} is unreleased, and would be the first published version."
            : project.Version < state.LatestVersion
                ? $"{project.Name} {project.Version} is unreleased — a backport (latest overall: {state.LatestVersion})."
                : $"{project.Name} {project.Version} is unreleased and ahead of {state.LatestVersionInLine?.ToString() ?? "everything published"}."
    };

    private static string NextMove(Project project, ReleaseState state, Changelog changelog)
    {
        var unreleased = changelog.Unreleased is { IsEmpty: false };
        if (state is { Published: true, LatestInLine: true })
        {
            return unreleased
                ? "[Unreleased] holds changes for the next release: bump <Version> and move them under it to prepare one."
                : "Nothing is waiting to ship.";
        }

        if (!state.LatestInLine)
        {
            return "Bump <Version> above the line's latest to make the project releasable.";
        }

        var entry = project.IsPrerelease ? changelog.Unreleased : changelog.Entry(project.Version);
        return entry is { IsEmpty: false }
            ? "Its changelog entry is present; the version is ready to deploy."
            : "It still needs a changelog entry before it can ship.";
    }
}
