using Microsoft.Extensions.Options;
using Ritten.Commands;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Engine.FileSystem;
using Ritten.NSchema;
using Ritten.Tests.Support;

namespace Ritten.Tests.NSchema;

public class NSchemaClientTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("ritten-nschema-").FullName;
    private readonly FakeCommandRunner _commands = new();
    private readonly ISecretProvider _secretProvider = Substitute.For<ISecretProvider>();
    private readonly NSchemaOptions _options = new();
    private static readonly NSchemaProject Project = new("schema");

    public NSchemaClientTests()
    {
        _secretProvider.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<string>().StartsWith("op://", StringComparison.Ordinal) ? $"value-of-{call.Arg<string>()}" : call.Arg<string>());
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private NSchemaClient NSchema() => new(_commands, Options.Create(_options), _secretProvider);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private IFile Write(string name, string content)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, content);
        return new PhysicalFile(path);
    }

    private Command Ran(string subcommand) => _commands.Executed.Single(command => command.Arguments.Contains(subcommand));

    [Fact]
    public async Task TheLocalTool_IsRestoredOnce_ThenRunThroughDotnet_InTheProject()
    {
        await NSchema().Plan(Project, ct: Ct);
        var client = NSchema();
        await client.Plan(Project, ct: Ct);
        await client.Apply(Project, ct: Ct);

        var restores = _commands.Executed.Where(command => command.Arguments is ["tool", "restore"]).ToList();
        restores.Count.ShouldBe(2);
        restores.ShouldAllBe(command => command.WorkingDirectory == "schema");
        var apply = Ran("apply");
        apply.Path.ShouldBe("dotnet");
        apply.Arguments[0].ShouldBe("nschema");
        apply.WorkingDirectory.ShouldBe("schema");
    }

    [Fact]
    public async Task AnInstalledExecutable_RunsWithoutRestoring_AndWithoutFetchingPlugins()
    {
        _options.Executable = "/opt/nschema/nschema";
        _options.NoInit = true;

        await NSchema().Apply(Project, ct: Ct);

        var apply = _commands.Executed.ShouldHaveSingleItem();
        apply.Path.ShouldBe("/opt/nschema/nschema");
        apply.Arguments.ShouldBe(["apply", "--auto-approve", "--no-color", "--no-init"]);
    }

    [Fact]
    public async Task Plan_SaysWhetherAnythingWouldChange_AndFailsOnAnythingElse()
    {
        _commands.Respond(command => command.Arguments.Contains("plan"), new CommandResult(2, "| create table |", ""));
        var changes = await NSchema().Plan(Project, ct: Ct);

        changes.ShouldBe(new NSchemaPlanResult(true, "| create table |"));
        Ran("plan").Arguments.ShouldBe(["nschema", "plan", "--detailed-exitcode", "--format", "markdown", "--no-color"]);

        var broken = new FakeCommandRunner();
        broken.Respond(command => command.Arguments.Contains("plan"), new CommandResult(1, "", "state-unreachable"));
        await Should.ThrowAsync<CommandFailedException>(() => new NSchemaClient(broken, Options.Create(_options)).Plan(Project, ct: Ct));
    }

    [Fact]
    public async Task Plan_WithNoChanges_IsQuiet()
    {
        (await NSchema().Plan(Project, ct: Ct)).HasChanges.ShouldBeFalse();
    }

    [Fact]
    public async Task Apply_TakesTheEnvironment_AndTheSettings()
    {
        await NSchema().Apply(Project, new NSchemaEnvironment { Name = "pi" },
            new NSchemaApplySettings { NoLock = true, DestructiveActions = NSchemaDestructiveActions.Warn }, Ct);

        Ran("apply").Arguments.ShouldBe(["nschema", "apply", "--auto-approve", "--no-color", "--environment", "pi", "--no-lock", "--destructive-actions", "warn"]);
    }

    [Fact]
    public async Task AFailedApply_Throws()
    {
        _commands.Respond(command => command.Arguments.Contains("apply"), new CommandResult(1, "", "requires-recreate"));

        await Should.ThrowAsync<CommandFailedException>(() => NSchema().Apply(Project, ct: Ct));
    }

    [Fact]
    public async Task TheEnvFilesAndVariables_ReachTheCommand_TheVariablesWinning()
    {
        var environment = new NSchemaEnvironment
        {
            EnvFiles = [Write("state.env", "AWS_ACCESS_KEY_ID=\"op://Vault/state/key\"\nNSCHEMA_STATE_PATH=from-file\n")],
            Variables = new Dictionary<string, string> { ["NSCHEMA_STATE_PATH"] = "/state/config.nschema.json" }
        };

        await NSchema().Apply(Project, environment, ct: Ct);

        var variables = Ran("apply").EnvironmentVariables;
        variables["AWS_ACCESS_KEY_ID"].ShouldBe("value-of-op://Vault/state/key");
        variables["NSCHEMA_STATE_PATH"].ShouldBe("/state/config.nschema.json");
    }

    [Fact]
    public async Task AnEnvFile_NeedsASecretsStore()
    {
        var environment = new NSchemaEnvironment { EnvFiles = [Write("state.env", "A=b\n")] };

        await Should.ThrowAsync<InvalidOperationException>(() => new NSchemaClient(_commands, Options.Create(_options)).Apply(Project, environment, ct: Ct));
    }

    [Fact]
    public async Task VerifyFormatting_NamesTheFiles_FormatCheckLists()
    {
        _commands.Respond(command => command.Arguments.Contains("format"), new CommandResult(2, "tables.sql\nviews.sql\n", ""));

        var unformatted = await NSchema().VerifyFormatting(Project, Ct);

        unformatted.ShouldBe(["tables.sql", "views.sql"]);
        Ran("format").Arguments.ShouldBe(["nschema", "format", "--check", ".", "--no-color"]);
    }

    [Fact]
    public async Task VerifyFormatting_Fails_OnAFileFormatPassesOver_ThatValidateRefuses()
    {
        // format --check skips a file it cannot parse and exits 0; validate is what notices.
        _commands.Respond(command => command.Arguments.Contains("validate"), new CommandResult(1, "", "tables.sql(3,1): expected ')'"));

        await Should.ThrowAsync<CommandFailedException>(() => NSchema().VerifyFormatting(Project, Ct));
    }

    [Fact]
    public async Task VerifyFormatting_IsNull_WhenEveryFileIsFormattedAndValid()
    {
        (await NSchema().VerifyFormatting(Project, Ct)).ShouldBeNull();
    }
}
