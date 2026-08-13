using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.DotNet;
using Ritten.NuGet;
using Ritten.Pipelines.NuGet;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Pipelines;

public class NuGetPushTests
{
    private readonly INuGet _nuget = Substitute.For<INuGet>();
    private readonly IPipelineContext _context = Substitute.For<IPipelineContext>();
    private readonly IBuildReport _report = Substitute.For<IBuildReport>();
    private readonly ReportSection _releaseSection = new("Release");
    private readonly NuGetOptions _options = TestOptions.NuGet();
    private readonly IFile _package = Substitute.For<IFile>();

    public NuGetPushTests()
    {
        _report.Section("Release").Returns(_releaseSection);
        _context.State.Get<PackResult>(Arg.Any<string>()).Returns(new PackResult { Packages = [_package] });
        _context.State.Get<Project>(Arg.Any<string>())
            .Returns(new Project { Name = "My.Package", Version = NuGetVersion.Parse("1.2.0") });
    }

    [Fact]
    public async Task ThrowsAClearErrorWithoutAnApiKey()
    {
        _options.ApiKey = null;

        var exception = await Should.ThrowAsync<Exception>(() => Step().Run(TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("NuGet__ApiKey");
        await _nuget.DidNotReceiveWithAnyArgs().Push(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PushesThePackedPackagesWithTheConfiguredFeed()
    {
        await Step().Run(TestContext.Current.CancellationToken);

        await _nuget.Received().Push(
            Arg.Is<NuGetFeed>(f => f.Url == _options.Feed && f.ApiKey == _options.ApiKey),
            _package,
            Arg.Any<CancellationToken>());
        _releaseSection.Tone.ShouldBe(ReportTone.Success);
    }

    private NuGetPush Step() =>
        new(Options.Create(_options), _context, _nuget, _report);
}
