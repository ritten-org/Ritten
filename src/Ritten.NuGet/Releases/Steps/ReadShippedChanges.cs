using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Git;
using Ritten.Reporting;

namespace Ritten.Releases.Steps;

/// <summary>
/// Reads which of the files that decide what the packages contain a pull request changed.
/// </summary>
/// <param name="pullRequest">The pull request under review, if any.</param>
/// <param name="git">The git client.</param>
/// <param name="fileSystem">The file system, whose project root is the repository asked.</param>
/// <param name="log">The workflow log.</param>
[Step("read shipped changes", StepKind.Work)]
public class ReadShippedChanges(PullRequest pullRequest, IGit git, IFileSystem fileSystem, IWorkflowLog log)
{
    /// <summary>
    /// The repository-wide files that change every project's output: its build properties and its package versions.
    /// </summary>
    private static readonly string[] SharedInputs = ["Directory.Build.props", "Directory.Packages.props"];

    /// <summary>
    /// The remote a pull request's base is fetched from.
    /// </summary>
    private const string Remote = "origin";

    /// <summary>
    /// Diffs each shipped project's directory, and the shared build inputs, against the pull request's base.
    /// </summary>
    /// <param name="release">How the project releases: its feed, release lines and cadence.</param>
    /// <param name="packages">The packages the repository ships (see <see cref="ReadProjects"/>).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult<ShippedChanges>> Run(ReleaseSettings release, PackageSet packages, CancellationToken cancellationToken = default)
    {
        if (pullRequest.BaseRef is not { Length: > 0 } baseRef)
        {
            log.Detail("Not reviewing a pull request; nothing to measure changes against.");
            return ShippedChanges.Unreviewed;
        }

        if (release.Cadence != ReleaseCadence.Continuous)
        {
            log.Detail($"A {release.Cadence.ToString().ToLowerInvariant()} release doesn't judge what a pull request changed; not measured.");
            return ShippedChanges.Unreviewed;
        }

        // A CI checkout is shallow and holds only the commit under test, so the base is fetched
        // first — with history enough to find where this branch left it.
        await git.FetchMergeBase(fileSystem.ProjectRoot, Remote, baseRef, cancellationToken);

        // A project's directory is the honest unit: its sources, its embedded resources and the
        // project file itself all ship. A project it references without packing is not covered —
        // in a lockstep repository every referenced project ships and is listed anyway.
        var paths = packages.Packages
            .Select(p => Path.GetDirectoryName(p.ProjectFile) is { Length: > 0 } directory ? directory : ".")
            .Concat(SharedInputs)
            .Distinct();

        List<string> files = [];
        foreach (var path in paths)
        {
            files.AddRange(await git.ChangedFilesSince(fileSystem.ProjectRoot, $"{Remote}/{baseRef}", path, cancellationToken));
        }

        var changes = new ShippedChanges(baseRef, [.. files.Distinct()]);
        log.Detail(changes.Any
            ? $"{changes.Files.Count} shipped file(s) changed since {baseRef}."
            : $"Nothing shipped changed since {baseRef}.");
        return changes;
    }
}
