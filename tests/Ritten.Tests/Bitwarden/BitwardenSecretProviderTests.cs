using Microsoft.Extensions.Options;
using Ritten.Bitwarden;
using Ritten.Commands;
using Ritten.Reporting;

namespace Ritten.Tests.Bitwarden;

public class BitwardenSecretProviderTests
{
    private const string Server = "https://vault.example.com";
    private const string Unauthenticated = """{"serverUrl":null,"status":"unauthenticated"}""";
    private const string Collections = """[{"id":"c1","name":"Lab"},{"id":"c2","name":"Other"}]""";
    private const string Items = """
        [
          {"id":"i1","name":"beszel","collectionIds":["c1"],"login":{"username":"admin","password":"hub-password"},"fields":[]},
          {"id":"i2","name":"beszel-agent","collectionIds":["c1"],"login":null,"notes":"the agent","fields":[{"name":"credential","value":"agent-token"}]},
          {"id":"i3","name":"twin","collectionIds":["c1"],"login":{"password":"one"},"fields":[]},
          {"id":"i4","name":"twin","collectionIds":["c1"],"login":{"password":"two"},"fields":[]},
          {"id":"i5","name":"elsewhere","collectionIds":["c2"],"login":{"password":"not-lab"},"fields":[]}
        ]
        """;

    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();
    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();
    private readonly BitwardenOptions _options = new() { ServerUrl = Server };
    private readonly Dictionary<string, string> _environment = new()
    {
        ["BW_CLIENTID"] = "user.u1",
        ["BW_CLIENTSECRET"] = "client-secret",
        ["BW_PASSWORD"] = "pa$$word",
    };
    private readonly Dictionary<string, CommandResult> _replies = new()
    {
        ["status"] = new CommandResult(0, Unauthenticated, ""),
        ["unlock"] = new CommandResult(0, "session-key\n", ""),
        ["list collections"] = new CommandResult(0, Collections, ""),
        ["list items"] = new CommandResult(0, Items, ""),
    };

    public BitwardenSecretProviderTests() =>
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>()).Returns(call => Reply(call.Arg<Command>()));

    [Theory]
    [InlineData("literal")]
    [InlineData("op://Vault/item/field")]
    [InlineData("https://example.com")]
    public async Task Resolve_PassesWhatIsNotItsThrough(string value)
    {
        (await Resolve(value)).ShouldBe(value);
        _commands.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("bw://Lab/item")]
    [InlineData("bw://Lab//field")]
    [InlineData("bw://Lab/item/field/extra")]
    public async Task Resolve_RefusesAMalformedReferenceRatherThanPassingItThrough(string value)
    {
        var failure = await Should.ThrowAsync<InvalidOperationException>(() => Resolve(value));

        failure.Message.ShouldContain(value);
        _commands.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Resolve_LogsAMachineInPointsItAtItsServerAndReadsTheVault()
    {
        (await Resolve("bw://Lab/beszel/password")).ShouldBe("hub-password");

        Sent().Select(command => string.Join(' ', command.Arguments)).ShouldBe(
        [
            "status --nointeraction",
            $"config server {Server} --nointeraction",
            "login --apikey --nointeraction",
            "unlock --passwordenv BW_PASSWORD --raw --nointeraction",
            "sync --nointeraction",
            "list collections --nointeraction",
            "list items --nointeraction",
        ]);
        Sent().ShouldAllBe(command => command.Path == "bw" && command.OutputRedacted);
    }

    [Fact]
    public async Task Resolve_HandsEachCredentialOnlyToTheCommandThatNeedsIt()
    {
        await Resolve("bw://Lab/beszel/password");

        var login = Sent("login");
        login.EnvironmentVariables["BW_CLIENTID"].ShouldBe("user.u1");
        login.EnvironmentVariables["BW_CLIENTSECRET"].ShouldBe("client-secret");
        login.EnvironmentVariables.ShouldNotContainKey("BW_PASSWORD");
        var unlock = Sent("unlock");
        unlock.EnvironmentVariables["BW_PASSWORD"].ShouldBe("pa$$word");
        unlock.EnvironmentVariables.ShouldNotContainKey("BW_CLIENTSECRET");
        Sent("list").EnvironmentVariables["BW_SESSION"].ShouldBe("session-key");
        Sent("list").EnvironmentVariables.ShouldNotContainKey("BW_PASSWORD");
    }

    [Fact]
    public async Task Resolve_ReadsTheVaultOncePerRun()
    {
        using var secrets = Secrets();
        await secrets.Resolve("bw://Lab/beszel/password", TestContext.Current.CancellationToken);
        await secrets.Resolve("bw://Lab/beszel-agent/credential", TestContext.Current.CancellationToken);

        Sent().Count(command => command.Arguments is ["list", "items", ..]).ShouldBe(1);
    }

    [Theory]
    [InlineData("bw://Lab/beszel/username", "admin")]
    [InlineData("bw://Lab/beszel-agent/credential", "agent-token")]
    [InlineData("bw://Lab/beszel-agent/Credential", "agent-token")]
    [InlineData("bw://Lab/beszel-agent/notes", "the agent")]
    public async Task Resolve_ReadsTheItemsOwnFieldsAndItsCustomOnes(string reference, string expected) =>
        (await Resolve(reference)).ShouldBe(expected);

    [Fact]
    public async Task Resolve_MatchesAnItemsNameExactly()
    {
        // `bw get item beszel` would find beszel-agent too, and refuse to choose.
        (await Resolve("bw://Lab/beszel/password")).ShouldBe("hub-password");
    }

    [Fact]
    public async Task Resolve_RefusesItemsThatShareAName()
    {
        var failure = await Should.ThrowAsync<InvalidOperationException>(() => Resolve("bw://Lab/twin/password"));

        failure.Message.ShouldContain("2 items named 'twin'");
    }

    [Fact]
    public async Task Resolve_ReadsOnlyTheNamedCollection()
    {
        var failure = await Should.ThrowAsync<InvalidOperationException>(() => Resolve("bw://Lab/elsewhere/password"));

        failure.Message.ShouldContain("'Lab' has no item named 'elsewhere'");
    }

    [Fact]
    public async Task Resolve_SaysWhichFieldsAnItemHasWhenItHasNotTheOneAskedFor()
    {
        var failure = await Should.ThrowAsync<InvalidOperationException>(() => Resolve("bw://Lab/beszel-agent/token"));

        failure.Message.ShouldContain("'notes', 'credential'");
        failure.Message.ShouldNotContain("agent-token");
    }

    [Fact]
    public async Task Resolve_SaysWhenACollectionIsNotSharedWithTheUser()
    {
        var failure = await Should.ThrowAsync<InvalidOperationException>(() => Resolve("bw://Missing/beszel/password"));

        failure.Message.ShouldContain("no collection named 'Missing'");
    }

    [Fact]
    public async Task Resolve_StaysLoggedInAsTheSameUserOnTheSameServer()
    {
        _replies["status"] = new CommandResult(0, $$"""{"serverUrl":"{{Server}}/","userId":"u1","status":"locked","lastSync":"2026-11-18T09:00:00Z"}""", "");

        await Resolve("bw://Lab/beszel/password");

        Sent().Select(command => command.Arguments[0]).ShouldBe(["status", "unlock", "sync", "list", "list"]);
    }

    [Fact]
    public async Task Resolve_LogsOutWhoeverElseIsLoggedIn()
    {
        _replies["status"] = new CommandResult(0, $$"""{"serverUrl":"{{Server}}","userId":"someone-else","status":"locked"}""", "");

        await Resolve("bw://Lab/beszel/password");

        Sent().Select(command => command.Arguments[0]).ShouldBe(["status", "logout", "login", "unlock", "sync", "list", "list"]);
    }

    [Fact]
    public async Task Resolve_MovesToItsServerLoggedOut()
    {
        // bw refuses to change server with anyone logged in.
        _replies["status"] = new CommandResult(0, """{"serverUrl":"https://elsewhere.example.com","userId":"u1","status":"locked"}""", "");

        await Resolve("bw://Lab/beszel/password");

        Sent().Select(command => command.Arguments[0]).ShouldBe(["status", "logout", "config", "login", "unlock", "sync", "list", "list"]);
    }

    [Fact]
    public async Task Resolve_ReadsTheLastCopyWhenTheServerCannotBeReached()
    {
        _replies["status"] = new CommandResult(0, $$"""{"serverUrl":"{{Server}}","userId":"u1","status":"locked","lastSync":"2026-11-18T09:00:00Z"}""", "");
        _replies["sync"] = new CommandResult(1, "", "Syncing failed: FetchError: request to https://vault.example.com failed");

        (await Resolve("bw://Lab/beszel/password")).ShouldBe("hub-password");

        _log.Received(1).Log(WorkflowLogLevel.Warning, Arg.Is<string>(message => message.Contains("2026-11-18 09:00")), null);
    }

    [Fact]
    public async Task Resolve_FailsWhenTheServerCannotBeReachedAndThereIsNoCopy()
    {
        _replies["sync"] = new CommandResult(1, "", "Syncing failed: FetchError");

        var failure = await Should.ThrowAsync<CommandFailedException>(() => Resolve("bw://Lab/beszel/password"));

        failure.Message.ShouldContain("no copy");
    }

    [Fact]
    public async Task Resolve_SaysWhyBwRefused()
    {
        _replies["login"] = new CommandResult(1, "", "invalid_client");

        var failure = await Should.ThrowAsync<CommandFailedException>(() => Resolve("bw://Lab/beszel/password"));

        failure.Message.ShouldBe("bw login failed: invalid_client");
    }

    [Fact]
    public async Task Resolve_ReadsTheCredentialsFileLiterally()
    {
        // A shell sourcing the file would make something else of the $ and the backslash.
        var file = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(
                file,
                "BW_CLIENTID=user.u1\nBW_CLIENTSECRET=client-secret\nBW_PASSWORD=pa$HOME\\word\n",
                TestContext.Current.CancellationToken);
            _environment.Clear();
            _options.CredentialsFile = file;

            await Resolve("bw://Lab/beszel/password");

            Sent("unlock").EnvironmentVariables["BW_PASSWORD"].ShouldBe("pa$HOME\\word");
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task Resolve_RefusesACredentialsFileWithoutAllThree()
    {
        var file = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(file, "BW_CLIENTID=user.u1\n", TestContext.Current.CancellationToken);
            _environment.Clear();
            _options.CredentialsFile = file;

            var failure = await Should.ThrowAsync<InvalidOperationException>(() => Resolve("bw://Lab/beszel/password"));

            failure.Message.ShouldContain("BW_CLIENTSECRET, BW_PASSWORD");
            _commands.ReceivedCalls().ShouldBeEmpty();
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task Resolve_UsesASessionSomeoneUnlockedWhenThereAreNoCredentials()
    {
        _environment.Clear();
        _replies["status"] = new CommandResult(0, $$"""{"serverUrl":"{{Server}}","userId":"me","status":"unlocked","lastSync":"2026-11-18T09:00:00Z"}""", "");

        (await Resolve("bw://Lab/beszel/password")).ShouldBe("hub-password");

        Sent().Select(command => command.Arguments[0]).ShouldBe(["status", "sync", "list", "list"]);
        Sent().ShouldAllBe(command => !command.EnvironmentVariables.ContainsKey("BW_SESSION"));
    }

    [Fact]
    public async Task Resolve_SaysHowToUnlockWhenThereIsNeitherCredentialsNorASession()
    {
        _environment.Clear();
        _replies["status"] = new CommandResult(0, $$"""{"serverUrl":"{{Server}}","userId":"me","status":"locked"}""", "");

        var failure = await Should.ThrowAsync<InvalidOperationException>(() => Resolve("bw://Lab/beszel/password"));

        failure.Message.ShouldContain("bw unlock --raw");
    }

    [Fact]
    public async Task Resolve_NeverPointsSomeonesOwnSessionAtAnotherServer()
    {
        _environment.Clear();
        _replies["status"] = new CommandResult(0, """{"serverUrl":"https://elsewhere.example.com","userId":"me","status":"unlocked"}""", "");

        var failure = await Should.ThrowAsync<InvalidOperationException>(() => Resolve("bw://Lab/beszel/password"));

        failure.Message.ShouldContain("elsewhere.example.com");
        Sent().Select(command => command.Arguments[0]).ShouldBe(["status"]);
    }

    [Fact]
    public async Task Resolve_KeepsTheCliInItsOwnDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ritten-bw-{Guid.NewGuid():N}");
        try
        {
            _options.DataDirectory = directory;

            await Resolve("bw://Lab/beszel/password");

            Sent().ShouldAllBe(command => command.EnvironmentVariables["BITWARDENCLI_APPDATA_DIR"] == directory);
            Directory.Exists(directory).ShouldBeTrue();
            if (!OperatingSystem.IsWindows())
            {
                File.GetUnixFileMode(directory).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private async Task<string> Resolve(string value)
    {
        using var secrets = Secrets();
        return await secrets.Resolve(value, TestContext.Current.CancellationToken);
    }

    private BitwardenSecretProvider Secrets() =>
        new(_commands, Options.Create(_options), _log, name => _environment.GetValueOrDefault(name));

    private CommandResult Reply(Command command)
    {
        var key = command.Arguments[0] == "list" ? $"list {command.Arguments[1]}" : command.Arguments[0];
        return _replies.GetValueOrDefault(key) ?? new CommandResult(0, "", "");
    }

    private List<Command> Sent() => [.. _commands.ReceivedCalls().Select(call => (Command)call.GetArguments()[0]!)];

    private Command Sent(string verb) => Sent().First(command => command.Arguments[0] == verb);
}
