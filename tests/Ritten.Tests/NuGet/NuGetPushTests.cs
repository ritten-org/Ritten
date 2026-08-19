using NuGet.Versioning;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.NuGet;
using Ritten.NuGet.Steps;
using Ritten.Releases;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.NuGet;

public class NugetPushTests
{
    private static readonly Project TheProject = new() { Name = "My.Package", Version = NuGetVersion.Parse("1.2.0") };

    private readonly INuGet _nuget = Substitute.For<INuGet>();
    private readonly IWorkflowReport _report = Substitute.For<IWorkflowReport>();
    private readonly ReportSection _releaseSection = new("Release");
    private readonly NuGetOptions _options = TestOptions.NuGet();
    private readonly IFile _package = Substitute.For<IFile>();

    public NugetPushTests()
    {
        _report.Section("Release").Returns(_releaseSection);
    }

    [Fact]
    public async Task PushesThePackedPackagesToTheAuthenticatedFeed()
    {
        var feed = new NuGetFeed(_options.Feed).WithApiKey("the-key");
        var packed = new PackResult { Packages = [_package] };

        var release = new ReleaseState(false, true, null, null) { Packages = [new("My.Package", false)] };

        await Step().Run(feed, packed, TheProject, release, TestContext.Current.CancellationToken);

        await _nuget.Received().Push(feed, _package, Arg.Any<CancellationToken>());
        _releaseSection.Tone.ShouldBe(ReportTone.Success);
    }

    [Fact]
    public async Task SkipsPackagesAlreadyOnTheFeed()
    {
        // A half-failed deploy leaves some packages up; the rerun finishes the release rather
        // than tripping over what's already there. Matching is exact, so a published package
        // can't shadow another that shares its name as a prefix.
        var feed = new NuGetFeed(_options.Feed).WithApiKey("the-key");
        var core = Substitute.For<IFile>();
        core.Name.Returns("My.Package.Core.1.2.0.nupkg");
        var tool = Substitute.For<IFile>();
        tool.Name.Returns("My.Package.1.2.0.nupkg");
        var packed = new PackResult { Packages = [core, tool] };
        var release = new ReleaseState(false, true, null, null)
        {
            Packages = [new("My.Package.Core", false), new("My.Package", true)]
        };

        await Step().Run(feed, packed, TheProject, release, TestContext.Current.CancellationToken);

        await _nuget.Received().Push(feed, core, Arg.Any<CancellationToken>());
        await _nuget.DidNotReceive().Push(feed, tool, Arg.Any<CancellationToken>());
        _releaseSection.Tone.ShouldBe(ReportTone.Success);
    }

    private NugetPush Step() =>
        new(Substitute.For<IWorkflowLog>(), _nuget, _report);
}
