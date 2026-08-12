using Hamelin;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Build.Steps;
using Wolfe.Hamelin.Build.Tests.Support;
using Wolfe.Hamelin.DotNet;
using Wolfe.Hamelin.Git;

namespace Wolfe.Hamelin.Build.Tests.Steps;

public class CreateTagTests
{
    private readonly IGit _git = Substitute.For<IGit>();
    private readonly IPipelineContext _context = Substitute.For<IPipelineContext>();
    private readonly ReleaseOptions _options = TestOptions.Release();

    public CreateTagTests()
    {
        _context.State.Get<Project>(Arg.Any<string>())
            .Returns(new Project { Name = "My.Package", Version = NuGetVersion.Parse("1.2.0") });
    }

    [Fact]
    public async Task SkipsWhenTheTagAlreadyExistsOnOrigin()
    {
        _git.RemoteTagExists("origin", "v1.2.0", Arg.Any<CancellationToken>()).Returns(true);

        await Step().Run(TestContext.Current.CancellationToken);

        await _git.DidNotReceiveWithAnyArgs().CreateTag(default!, default, TestContext.Current.CancellationToken);
        await _git.DidNotReceiveWithAnyArgs().PushTag(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreatesAndPushesTheTagWhenItDoesNotExist()
    {
        await Step().Run(TestContext.Current.CancellationToken);

        await _git.Received().CreateTag("v1.2.0", null, Arg.Any<CancellationToken>());
        await _git.Received().PushTag("origin", "v1.2.0", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TagsTheConfiguredCommitSha()
    {
        _options.CommitSha = "abc123";

        await Step().Run(TestContext.Current.CancellationToken);

        await _git.Received().CreateTag("v1.2.0", "abc123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushesAnExistingLocalTagWithoutRecreatingIt()
    {
        _git.TagExists("v1.2.0", Arg.Any<CancellationToken>()).Returns(true);

        await Step().Run(TestContext.Current.CancellationToken);

        await _git.DidNotReceiveWithAnyArgs().CreateTag(default!, default, TestContext.Current.CancellationToken);
        await _git.Received().PushTag("origin", "v1.2.0", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HonoursTheTagPrefix()
    {
        _options.TagPrefix = "release/";

        await Step().Run(TestContext.Current.CancellationToken);

        await _git.Received().CreateTag("release/1.2.0", null, Arg.Any<CancellationToken>());
    }

    private CreateTag Step() =>
        new(NullLogger<CreateTag>.Instance, Options.Create(_options), _context, _git);
}
