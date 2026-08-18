using NuGet.Versioning;
using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Workflows.Steps;
using Ritten.Releases;

namespace Ritten.Tests.Workflows;

/// <summary>
/// Status observes and never judges: every state maps to a sentence and a next move, and the
/// step always succeeds.
/// </summary>
public class StatusReportTests
{
    private static readonly Changelog Empty = new();

    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();

    [Fact]
    public void AtRestWithPendingChanges_PointsAtPreparingARelease()
    {
        var state = new ReleaseState(Published: true, LatestInLine: true, NuGetVersion.Parse("1.2.0"), NuGetVersion.Parse("1.2.0"));
        var changelog = new Changelog { Entries = [new ChangelogEntry { Added = ["A change."] }] };

        var result = Step().Run(Project("1.2.0"), state, changelog);

        result.IsFailure.ShouldBeFalse();
        Said("published and at rest");
        Said("[Unreleased] holds changes");
    }

    [Fact]
    public void AtRestWithNothingPending_SaysSo()
    {
        var state = new ReleaseState(Published: true, LatestInLine: true, NuGetVersion.Parse("1.2.0"), NuGetVersion.Parse("1.2.0"));

        Step().Run(Project("1.2.0"), state, Empty);

        Said("Nothing is waiting to ship");
    }

    [Fact]
    public void ReleasableWithAnEntry_IsReadyToDeploy()
    {
        var state = new ReleaseState(Published: false, LatestInLine: true, NuGetVersion.Parse("1.1.0"), NuGetVersion.Parse("1.1.0"));
        var changelog = new Changelog
        {
            Entries = [new ChangelogEntry { Version = NuGetVersion.Parse("1.2.0"), Added = ["A change."] }]
        };

        Step().Run(Project("1.2.0"), state, changelog);

        Said("unreleased and ahead of 1.1.0");
        Said("ready to deploy");
    }

    [Fact]
    public void ReleasableWithoutAnEntry_NamesTheMissingPiece()
    {
        var state = new ReleaseState(Published: false, LatestInLine: true, NuGetVersion.Parse("1.1.0"), NuGetVersion.Parse("1.1.0"));

        Step().Run(Project("1.2.0"), state, Empty);

        Said("still needs a changelog entry");
    }

    [Fact]
    public void AWoundBackVersion_IsCalledOut()
    {
        var state = new ReleaseState(Published: true, LatestInLine: false, NuGetVersion.Parse("1.3.0"), NuGetVersion.Parse("1.3.0"));

        var result = Step().Run(Project("1.2.0"), state, Empty);

        result.IsFailure.ShouldBeFalse();
        Said("wound back");
        Said("Bump <Version>");
    }

    [Fact]
    public void AnOvertakenVersion_IsCalledOut()
    {
        var state = new ReleaseState(Published: false, LatestInLine: false, NuGetVersion.Parse("1.5.0"), NuGetVersion.Parse("1.5.0"));

        var result = Step().Run(Project("1.2.0"), state, Empty);

        result.IsFailure.ShouldBeFalse();
        Said("moved on to 1.5.0");
    }

    [Fact]
    public void TheFirstEverVersion_IsDescribed()
    {
        var state = new ReleaseState(Published: false, LatestInLine: true, null, null);

        Step().Run(Project("0.1.0"), state, Empty);

        Said("first published version");
    }

    private void Said(string fragment) =>
        _log.Received().Log(
            WorkflowLogLevel.Status,
            Arg.Is<string>(m => m.Contains(fragment)),
            Arg.Any<Exception>());

    private static Project Project(string version) =>
        new() { Name = "My.Package", Version = NuGetVersion.Parse(version) };

    private StatusReport Step() => new(_log);
}
