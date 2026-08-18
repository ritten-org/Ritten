using NuGet.Versioning;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.NuGet;
using Ritten.NuGet.Steps;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.NuGet;

public class NugetPushTests
{
    private static readonly Project TheProject = new() { Name = "My.Package", Version = NuGetVersion.Parse("1.2.0") };

    private readonly INuGet _nuget = Substitute.For<INuGet>();
    private readonly IBuildReport _report = Substitute.For<IBuildReport>();
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

        await Step().Run(feed, packed, TheProject, TestContext.Current.CancellationToken);

        await _nuget.Received().Push(feed, _package, Arg.Any<CancellationToken>());
        _releaseSection.Tone.ShouldBe(ReportTone.Success);
    }

    private NugetPush Step() =>
        new(_nuget, _report);
}
