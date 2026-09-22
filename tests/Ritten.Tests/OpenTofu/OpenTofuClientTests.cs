using Microsoft.Extensions.Options;
using Ritten.Commands;
using Ritten.Contracts;
using Ritten.Engine.FileSystem;
using Ritten.OpenTofu;
using Ritten.Tests.Support;

namespace Ritten.Tests.OpenTofu;

public class OpenTofuClientTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("ritten-tofu-").FullName;
    private readonly FakeCommandRunner _commands = new();
    private readonly ISecretProvider _secretProvider = Substitute.For<ISecretProvider>();
    private readonly OpenTofuOptions _options = new();

    public OpenTofuClientTests()
    {
        _secretProvider.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<string>().StartsWith("op://", StringComparison.Ordinal) ? $"value-of-{call.Arg<string>()}" : call.Arg<string>());
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public async Task Init_PreparesTheRootWithoutPrompting()
    {
        await Tofu().Init(ct: TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Path.ShouldBe("tofu");
        command.Arguments.ShouldBe(["init", "-input=false"]);
    }

    [Fact]
    public async Task ChdirComesBeforeTheSubcommand()
    {
        // The one place OpenTofu cares about argument order.
        _options.Root = "infra";

        await Tofu().Init(ct: TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments.ShouldBe(["-chdir=infra", "init", "-input=false"]);
    }

    [Fact]
    public async Task TheVarFileIsPerCallAndReachesEveryCommandThatTakesOne()
    {
        // Per call, not per client: one root module serves every environment it has files for.
        var lab = new TofuEnvironment { VarFile = Write("lab.tfvars", "") };
        var prod = new TofuEnvironment { VarFile = Write("prod.tfvars", "") };

        await Tofu().Init(lab, TestContext.Current.CancellationToken);
        await Tofu().Plan(lab, TestContext.Current.CancellationToken);
        await Tofu().Apply(prod, TestContext.Current.CancellationToken);

        _commands.Executed.Count.ShouldBe(3);
        _commands.Executed[0].Arguments.ShouldContain($"-var-file={lab.VarFile!.AbsolutePath}");
        _commands.Executed[1].Arguments.ShouldContain($"-var-file={lab.VarFile!.AbsolutePath}");
        _commands.Executed[2].Arguments.ShouldContain($"-var-file={prod.VarFile!.AbsolutePath}");
    }

    [Fact]
    public async Task TheEnvFilesRunEveryCommandUnderTheirResolvedValues()
    {
        var environment = new TofuEnvironment
        {
            EnvFiles =
            [
                Write("state.env", "AWS_ACCESS_KEY_ID=\"op://Vault/state/key\"\n"),
                Write("secrets.env", "TF_VAR_token=\"op://Vault/provider/token\"\nTF_VAR_region=literal\n")
            ]
        };

        await Tofu().Init(environment, TestContext.Current.CancellationToken);
        await Tofu().Plan(environment, TestContext.Current.CancellationToken);
        await Tofu().Apply(environment, TestContext.Current.CancellationToken);

        _commands.Executed.Count.ShouldBe(3);
        _commands.Executed.ShouldAllBe(c =>
            c.EnvironmentVariables["AWS_ACCESS_KEY_ID"] == "value-of-op://Vault/state/key"
            && c.EnvironmentVariables["TF_VAR_token"] == "value-of-op://Vault/provider/token"
            && c.EnvironmentVariables["TF_VAR_region"] == "literal");
    }

    [Fact]
    public async Task NoEnvironmentMeansTheProcessesOwn()
    {
        await Tofu().Plan(ct: TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.EnvironmentVariables.ShouldBeEmpty();
        command.Arguments.ShouldNotContain(a => a.StartsWith("-var-file", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AMalformedEnvFileFailsBeforeAnythingRuns()
    {
        var environment = new TofuEnvironment { EnvFiles = [Write("broken.env", "not an assignment\n")] };

        var failure = await Should.ThrowAsync<InvalidOperationException>(
            () => Tofu().Init(environment, TestContext.Current.CancellationToken));

        failure.Message.ShouldContain("broken.env:1");
        _commands.Executed.ShouldBeEmpty();
    }

    [Fact]
    public async Task Plan_ReportsNothingToDoOnAClean_Exit()
    {
        var plan = await Tofu().Plan(ct: TestContext.Current.CancellationToken);

        plan.HasChanges.ShouldBeFalse();
    }

    [Fact]
    public async Task Plan_ReportsChangesOnExitTwo()
    {
        _commands.Respond(c => c.Arguments.Contains("plan"), new CommandResult(2, "~ resource \"a\"", ""));

        var plan = await Tofu().Plan(ct: TestContext.Current.CancellationToken);

        plan.HasChanges.ShouldBeTrue();
        plan.Output.ShouldBe("~ resource \"a\"");
    }

    [Fact]
    public async Task Plan_AsksForTheThreeWaySplitAndDoesNotTreatItAsFailure()
    {
        // Without -detailed-exitcode a plan with changes and a plan that could not run are the
        // same code, so the command must not throw on a non-zero exit of its own accord.
        await Tofu().Plan(ct: TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.ShouldContain("-detailed-exitcode");
        command.ThrowsOnError.ShouldBeFalse();
    }

    [Fact]
    public async Task Plan_StillFailsWhenItCouldNotRun()
    {
        _commands.Respond(c => c.Arguments.Contains("plan"), new CommandResult(1, "", "no valid credential sources"));

        await Should.ThrowAsync<CommandFailedException>(
            async () => await Tofu().Plan(ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task VerifyFormatting_SaysNothingWhenEveryFileIsFormatted()
    {
        (await Tofu().VerifyFormatting(TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task VerifyFormatting_NamesTheFilesThatNeedIt()
    {
        _commands.Respond(c => c.Arguments.Contains("fmt"), new CommandResult(3, "main.tf\ndns.tf\n", ""));

        var unformatted = await Tofu().VerifyFormatting(TestContext.Current.CancellationToken);

        unformatted.ShouldBe(["main.tf", "dns.tf"]);
    }

    [Fact]
    public async Task VerifyFormatting_FailsRatherThanReportingCleanWhenItCouldNotReadTheRoot()
    {
        // A non-zero exit with nothing listed is a broken root, not a formatted one — and
        // returning null there would report "every file is formatted" about files it never read.
        _commands.Respond(c => c.Arguments.Contains("fmt"), new CommandResult(1, "", "Failed to read module directory"));

        await Should.ThrowAsync<CommandFailedException>(
            async () => await Tofu().VerifyFormatting(TestContext.Current.CancellationToken));
    }

    private OpenTofuClient Tofu() => new(_commands, _secretProvider, Options.Create(_options));

    private PhysicalFile Write(string name, string contents)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, contents);
        return new PhysicalFile(path);
    }
}
