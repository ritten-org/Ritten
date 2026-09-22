using Microsoft.Extensions.Options;
using Ritten.Commands;
using Ritten.OnePassword;

namespace Ritten.Tests.OnePassword;

public class OnePasswordSecretProviderTests
{
    private const string Reference = "op://Vault/item/field";
    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();
    private readonly OnePasswordOptions _options = new();

    [Theory]
    [InlineData("literal")]
    [InlineData("bw://Vault/item/field")]
    [InlineData("https://example.com")]
    public async Task Resolve_PassesWhatIsNotItsThrough(string value)
    {
        (await Secrets().Resolve(value, TestContext.Current.CancellationToken)).ShouldBe(value);
        _commands.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("op://Vault/item")]
    [InlineData("op://Vault//field")]
    public async Task Resolve_RefusesAMalformedReferenceRatherThanPassingItThrough(string value)
    {
        // The scheme makes it 1Password's, so a wrong shape is an error to fix, never a literal.
        var failure = await Should.ThrowAsync<InvalidOperationException>(() => Secrets().Resolve(value, TestContext.Current.CancellationToken));

        failure.Message.ShouldContain(value);
        _commands.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Resolve_ReturnsWhatOpPrints()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "s3cret", ""));

        var value = await Secrets().Resolve(Reference, TestContext.Current.CancellationToken);

        value.ShouldBe("s3cret");
        var command = (Command)_commands.ReceivedCalls().Single().GetArguments()[0]!;
        command.Path.ShouldBe("op");
        command.Arguments.ShouldBe(["read", "--no-newline", Reference]);
        command.OutputRedacted.ShouldBeTrue();
    }

    [Fact]
    public async Task Resolve_DropsTheLineTerminatorTheRunnerAppends()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "s3cret\n", ""));

        (await Secrets().Resolve(Reference, TestContext.Current.CancellationToken)).ShouldBe("s3cret");
    }

    [Fact]
    public async Task Resolve_SaysWhyWhenOpRefuses()
    {
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>())
            .Returns(new CommandResult(1, "", "[ERROR] 2026/09/18 could not read secret: \"item\" isn't an item in the \"Vault\" vault."));

        var failure = await Should.ThrowAsync<CommandFailedException>(() => Secrets().Resolve(Reference, TestContext.Current.CancellationToken));

        failure.Message.ShouldContain(Reference);
        failure.Message.ShouldContain("isn't an item");
    }

    [Fact]
    public async Task Resolve_HandsTheTokenFileToOpWhenTheEnvironmentHasNone()
    {
        // Only when the environment has no token of its own: a workstation signed into the app
        // has neither, and op asks the app.
        var file = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(file, "token-from-file\n", TestContext.Current.CancellationToken);
            _options.ServiceAccountTokenFile = file;
            _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(new CommandResult(0, "s3cret", ""));

            await Secrets().Resolve(Reference, TestContext.Current.CancellationToken);

            var command = (Command)_commands.ReceivedCalls().Single().GetArguments()[0]!;
            if (Environment.GetEnvironmentVariable("OP_SERVICE_ACCOUNT_TOKEN") is not { Length: > 0 })
            {
                command.EnvironmentVariables["OP_SERVICE_ACCOUNT_TOKEN"].ShouldBe("token-from-file");
            }
        }
        finally
        {
            File.Delete(file);
        }
    }

    private OnePasswordSecretProvider Secrets() => new(_commands, Options.Create(_options));
}
