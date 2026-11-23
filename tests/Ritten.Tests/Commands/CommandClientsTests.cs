using Microsoft.Extensions.Options;
using Ritten.Commands;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Docker;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.NSchema;
using Ritten.OpenTofu;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.Commands;

// Every command client runs any command of its tool's, as an extension method outside Ritten would build one: its
// own executable, with what the caller adds; and a dry run's command runner runs it only when it changes nothing.
public class CommandClientsTests
{
    public static TheoryData<string, string, string[]> Clients() => new()
    {
        { "git", "git", [] },
        { "docker", "docker", [] },
        { "dotnet", "dotnet", [] },
        { "npm", "npm", [] },
        { "nschema", "dotnet", ["nschema"] },
        { "tofu", "tofu", [] }
    };

    private static ICommandClient Client(string tool, ICommandRunner commands) => tool switch
    {
        "git" => new GitClient(commands),
        "docker" => new DockerClient(commands),
        "dotnet" => new DotNetClient(commands, Substitute.For<IFileSystem>()),
        "npm" => new global::Ritten.Npm.Npm(commands),
        "nschema" => new NSchemaClient(commands, Options.Create(new NSchemaOptions())),
        "tofu" => new OpenTofuClient(commands, Substitute.For<ISecretProvider>()),
        _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, null)
    };

    [Theory]
    [MemberData(nameof(Clients))]
    public async Task RunCommand_RunsTheToolsCommand_AsTheCallerMadeIt(string tool, string executable, string[] own)
    {
        var commands = new FakeCommandRunner();
        commands.Respond(_ => true, new CommandResult(3, "out", "err"));
        var variable = EnvironmentVariableName.From("TF_CLI_CONFIG_FILE");

        var result = await Client(tool, commands).RunCommand(
            command => command.AndArguments("version").InDirectory("/repo").AndEnvironmentVariable(variable, "/repo/tofu.tfrc"),
            TestContext.Current.CancellationToken);

        result.ExitCode.Value.ShouldBe(3);
        var ran = commands.Executed.ShouldHaveSingleItem();
        ran.Path.ShouldBe(executable);
        ran.Arguments.ShouldBe([.. own, "version"]);
        ran.WorkingDirectory.ShouldBe("/repo");
        ran.EnvironmentVariables.ShouldBe(EnvironmentVariables.Empty.With(variable, "/repo/tofu.tfrc"));
        ran.Effect.ShouldBe(CommandEffect.Change, "a command nobody marked is one a dry run does not run");
    }

    [Theory]
    [MemberData(nameof(Clients))]
    public async Task InADryRun_ACommandThatChangesNothingRuns_AndAnyOtherDoesNot(string tool, string _, string[] own)
    {
        var commands = new FakeCommandRunner();
        var client = Client(tool, new DryRunCommandRunner(Substitute.For<IWorkflowLog>(), commands));

        await client.RunCommand(command => command.AndArguments("status").WithEffect(CommandEffect.None), TestContext.Current.CancellationToken);
        await client.RunCommand(command => command.AndArguments("push"), TestContext.Current.CancellationToken);

        commands.Executed.ShouldHaveSingleItem().Arguments.ShouldBe([.. own, "status"]);
    }
}
