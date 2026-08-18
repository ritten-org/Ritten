using Microsoft.Extensions.Options;
using Octokit;
using Ritten.Commands;
using Ritten.Contracts;
using Ritten.GitHub;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.GitHub;

public class AmbientCredentialStoreTests
{
    private readonly FakeCommandRunner _commands = new();
    private readonly GitHubClientOptions _options = new();

    [Fact]
    public async Task UsesTheEnvironmentTokenWithoutAskingTheCli()
    {
        _options.Token = "env-token";

        var credentials = await Store().GetCredentials();

        credentials.Password.ShouldBe("env-token");
        _commands.Executed.ShouldBeEmpty();
    }

    [Fact]
    public async Task FallsBackToTheGhClisStoredLogin()
    {
        _commands.Respond(c => c.Path == "gh", new CommandResult(0, "gh-token\n", ""));

        var credentials = await Store().GetCredentials();

        credentials.Password.ShouldBe("gh-token");
        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.ShouldBe(["auth", "token"]);
        command.OutputRedacted.ShouldBeTrue("the output is the token itself, and must stay out of the logs");
    }

    [Fact]
    public async Task IsAnonymousWhenTheCliHasNoLogin()
    {
        _commands.Respond(c => c.Path == "gh", new CommandResult(1, "", "not logged in"));

        var credentials = await Store().GetCredentials();

        credentials.AuthenticationType.ShouldBe(AuthenticationType.Anonymous);
    }

    [Fact]
    public async Task IsAnonymousWhenTheCliIsNotInstalled()
    {
        var commands = Substitute.For<ICommandRunner>();
        commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>())
            .Returns<CommandResult>(_ => throw new System.ComponentModel.Win32Exception("no such file"));
        var store = new AmbientCredentialStore(Substitute.For<IWorkflowLog>(), Options.Create(_options), commands);

        var credentials = await store.GetCredentials();

        credentials.AuthenticationType.ShouldBe(AuthenticationType.Anonymous);
    }

    [Fact]
    public async Task ResolvesOnceAndCachesTheAnswer()
    {
        // Octokit asks for credentials on every request; the CLI must not run every time.
        _commands.Respond(c => c.Path == "gh", new CommandResult(0, "gh-token", ""));
        var store = Store();

        await store.GetCredentials();
        await store.GetCredentials();

        _commands.Executed.Count.ShouldBe(1);
    }

    private AmbientCredentialStore Store() =>
        new(Substitute.For<IWorkflowLog>(), Options.Create(_options), _commands);
}
