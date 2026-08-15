using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.NuGet;
using Ritten.NuGet.Steps;
using Ritten.Tests.Support;

namespace Ritten.Tests.NuGet;

/// <summary>
/// Credential resolution sits after the gates, so a run with nothing to publish never asks,
/// and before anything irreversible, so a run that can't publish fails first.
/// </summary>
public class NugetAuthenticateTests
{
    private readonly IPipelinePrompt _prompt = Substitute.For<IPipelinePrompt>();
    private readonly NuGetOptions _options = TestOptions.NuGet();

    [Fact]
    public async Task UsesTheConfiguredApiKeyWithoutAsking()
    {
        _options.ApiKey = "env-key";

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().ApiKey.ShouldBe("env-key");
        await _prompt.DidNotReceiveWithAnyArgs().Secret(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ProducesAKeylessFeedForADryRun()
    {
        _options.ApiKey = null;

        var result = await Step(dryRun: true).Run(TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().ApiKey.ShouldBeNull();
        await _prompt.DidNotReceiveWithAnyArgs().Secret(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FailsWhenThereIsNoTerminalToAskAt()
    {
        _options.ApiKey = null;
        _prompt.IsInteractive.Returns(false);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("RITTEN_NUGET_API_KEY");
    }

    [Fact]
    public async Task AsksAtTheTerminalWhenInteractive()
    {
        _options.ApiKey = null;
        _prompt.IsInteractive.Returns(true);
        _prompt.Secret(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("typed-key");

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().ApiKey.ShouldBe("typed-key");
    }

    [Fact]
    public async Task FailsWhenNothingIsEntered()
    {
        _options.ApiKey = null;
        _prompt.IsInteractive.Returns(true);
        _prompt.Secret(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
    }

    private NugetAuthenticate Step(bool dryRun = false) =>
        new(new PipelineJob("dotnet-tool", "deploy", DryRun: dryRun), Substitute.For<IPipelineLog>(), Options.Create(_options), _prompt);
}
