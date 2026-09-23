using Ritten.Contracts;
using Ritten.Git;
using Ritten.Reporting;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Reads which of the files that decide what the packages contain a pull request changed.
/// </summary>
/// <param name="pullRequest">The pull request under review, if any.</param>
/// <param name="git">The git client.</param>
/// <param name="log">The workflow log.</param>
[Step("read shipped changes", StepKind.Work)]
public class ReadShippedChanges(PullRequest pullRequest, IGit git, IWorkflowLog log)
{
    /// <summary>
    /// The repository-wide files that change every project's output: its build properties and its package versions.
    /// </summary>
    internal static readonly string[] SharedInputs = ["Directory.Build.props", "Directory.Packages.props"];

    /// <summary>
    /// Diffs each shipped project's directory, and the shared build inputs, against the pull request's base.
    /// </summary>
    /// <param name="packages">The packages the repository ships (see <see cref="ReadProjects"/>).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult<ShippedChanges>> Run(PackageSet packages, CancellationToken cancellationToken = default)
    {
        if (pullRequest.BaseRef is not { Length: > 0 } baseRef)
        {
            log.Detail("Not reviewing a pull request; nothing to measure changes against.");
            return ShippedChanges.Unreviewed;
        }

        // A project's directory is the honest unit: its sources, its embedded resources and the
        // project file itself all ship. A project it references without packing is not covered —
        // in a lockstep repository every referenced project ships and is listed anyway.
        var paths = packages.Packages
            .Select(p => Path.GetDirectoryName(p.ProjectFile) is { Length: > 0 } directory ? directory : ".")
            .Concat(SharedInputs)
            .Distinct();

        // The remote-tracking ref: a checkout fetches the base without checking it out.
        List<string> files = [];
        foreach (var path in paths)
        {
            files.AddRange(await git.ChangedFilesSince($"origin/{baseRef}", path, cancellationToken));
        }

        var changes = new ShippedChanges(baseRef, [.. files.Distinct()]);
        log.Detail(changes.Any
            ? $"{changes.Files.Count} shipped file(s) changed since {baseRef}."
            : $"Nothing shipped changed since {baseRef}.");
        return changes;
    }
}
