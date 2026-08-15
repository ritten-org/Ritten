using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.NuGet;
using Ritten.Pipelines.NuGet;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Pipelines;

public class NuGetPushTests
{
    private readonly INuGet _nuget = Substitute.For<INuGet>();
    private readonly IPipelineState _state = Substitute.For<IPipelineState>();
    private readonly IBuildReport _report = Substitute.For<IBuildReport>();
    private readonly ReportSection _releaseSection = new("Release");
    private readonly NuGetOptions _options = TestOptions.NuGet();
    private readonly IFile _package = Substitute.For<IFile>();

    public NuGetPushTests()
    {
        _report.Section("Release").Returns(_releaseSection);
        _state.Get<PackResult>().Returns(new PackResult { Packages = [_package] });
        _state.Get<Project>()
            .Returns(new Project { Name = "My.Package", Version = NuGetVersion.Parse("1.2.0") });
    }

    [Fact]
    public async Task FailsWithAClearErrorWithoutAnApiKey()
    {
        _options.ApiKey = null;

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("RITTEN_NUGET_API_KEY"));
        await _nuget.DidNotReceiveWithAnyArgs().Push(null!, null!, TestContext.Current.CancellationToken);
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

    [Fact]
    public async Task DoesNotDemandAnApiKeyForADryRun()
    {
        // Nothing is going to be pushed, so nothing needs authenticating.
        _options.ApiKey = null;

        var result = await Step(dryRun: true).Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
    }

    private NuGetPush Step(bool dryRun = false) =>
        new(new PipelineJob("Test", "deploy", dryRun), Options.Create(_options), _state, _nuget, _report);
}
