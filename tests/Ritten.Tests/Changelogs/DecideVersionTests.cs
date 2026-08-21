using NuGet.Versioning;
using Ritten.Changelogs;
using Ritten.Changelogs.Steps;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.Releases;
using Ritten.Reporting;

namespace Ritten.Tests.Changelogs;

public class DecideVersionTests
{
    private readonly IWorkflowPrompt _prompt = Substitute.For<IWorkflowPrompt>();

    public DecideVersionTests()
    {
        _prompt.IsInteractive.Returns(true);
        _prompt.Confirm(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
    }

    [Fact]
    public async Task TakesTheVersionTheCallerNames()
    {
        var result = await Step(version: "2.0.0").Run(Project("1.2.0"), Changelog(), Published(), TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Version.ShouldBe(NuGetVersion.Parse("2.0.0"));
        result.Value.Bumped.ShouldBeTrue();
        await _prompt.DidNotReceive().Confirm(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task KeepsAVersionThatIsDeclaredButNotPublished()
    {
        // The project was bumped and never shipped; bumping again would skip a version.
        var result = await Step().Run(Project("1.3.0"), Changelog(), Unpublished(), TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Version.ShouldBe(NuGetVersion.Parse("1.3.0"));
        result.Value.Bumped.ShouldBeFalse();
        await _prompt.DidNotReceive().Confirm(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DerivesFromTheUnreleasedNotesAndConfirms()
    {
        var result = await Step().Run(Project("1.2.0"), Changelog(new ChangelogEntry { Added = ["A thing."] }), Published(), TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Version.ShouldBe(NuGetVersion.Parse("1.3.0"));
        await _prompt.Received().Confirm(Arg.Is<string>(m => m.Contains("1.3.0")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StopsWhenTheDerivedVersionIsRefused()
    {
        _prompt.Confirm(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await Step().Run(Project("1.2.0"), Changelog(new ChangelogEntry { Added = ["A thing."] }), Published(), TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("--version");
    }

    [Fact]
    public async Task RefusesToGuessWithNobodyThereToAsk()
    {
        _prompt.IsInteractive.Returns(false);

        var result = await Step().Run(Project("1.2.0"), Changelog(new ChangelogEntry { Added = ["A thing."] }), Published(), TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("--auto-approve");
    }

    [Fact]
    public async Task TakesTheDerivedVersionWhenApprovedUpFront()
    {
        _prompt.IsInteractive.Returns(false);

        var result = await Step(autoApprove: true).Run(Project("1.2.0"), Changelog(new ChangelogEntry { Fixed = ["A thing."] }), Published(), TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Version.ShouldBe(NuGetVersion.Parse("1.2.1"));
    }

    private static Project Project(string version) =>
        new() { Name = "My.Package", Version = NuGetVersion.Parse(version) };

    private static Changelog Changelog(ChangelogEntry? unreleased = null) =>
        new() { Entries = unreleased is null ? [] : [unreleased] };

    private static ReleaseState Published() =>
        new(Published: true, LatestInLine: true, NuGetVersion.Parse("1.2.0"), NuGetVersion.Parse("1.2.0"));

    private static ReleaseState Unpublished() =>
        new(Published: false, LatestInLine: true, NuGetVersion.Parse("1.2.0"), NuGetVersion.Parse("1.2.0"));

    private DecideVersion Step(string? version = null, bool autoApprove = false) =>
        new(
            new WorkflowJob("dotnet tool", "prepare", AutoApprove: autoApprove),
            version is null ? RequestedVersion.None : new RequestedVersion(NuGetVersion.Parse(version)),
            Substitute.For<IWorkflowLog>(),
            _prompt);
}
