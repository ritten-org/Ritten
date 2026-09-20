using Microsoft.Extensions.Options;
using Ritten.Commands;
using Ritten.OpenTofu;
using Ritten.Tests.Support;

namespace Ritten.Tests.OpenTofu;

public class OpenTofuClientTests
{
    private readonly FakeCommandRunner _commands = new();
    private readonly OpenTofuOptions _options = new();

    [Fact]
    public async Task Init_PreparesTheRootWithoutPrompting()
    {
        await Tofu().Init(TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Path.ShouldBe("tofu");
        command.Arguments.ShouldBe(["init", "-input=false"]);
    }

    [Fact]
    public async Task ChdirComesBeforeTheSubcommand()
    {
        // The one place OpenTofu cares about argument order.
        _options.Root = "infra";

        await Tofu().Init(TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments.ShouldBe(["-chdir=infra", "init", "-input=false"]);
    }

    [Fact]
    public async Task TheVarFileReachesEveryCommandThatTakesOne()
    {
        _options.VarFile = "lab.tfvars";

        await Tofu().Init(TestContext.Current.CancellationToken);
        await Tofu().Apply(TestContext.Current.CancellationToken);

        _commands.Executed.ShouldAllBe(c => c.Arguments.Contains("-var-file=lab.tfvars"));
    }

    [Fact]
    public async Task Plan_ReportsNothingToDoOnAClean_Exit()
    {
        var plan = await Tofu().Plan(TestContext.Current.CancellationToken);

        plan.HasChanges.ShouldBeFalse();
    }

    [Fact]
    public async Task Plan_ReportsChangesOnExitTwo()
    {
        _commands.Respond(c => c.Arguments.Contains("plan"), new CommandResult(2, "~ resource \"a\"", ""));

        var plan = await Tofu().Plan(TestContext.Current.CancellationToken);

        plan.HasChanges.ShouldBeTrue();
        plan.Output.ShouldBe("~ resource \"a\"");
    }

    [Fact]
    public async Task Plan_AsksForTheThreeWaySplitAndDoesNotTreatItAsFailure()
    {
        // Without -detailed-exitcode a plan with changes and a plan that could not run are the
        // same code, so the command must not throw on a non-zero exit of its own accord.
        await Tofu().Plan(TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.ShouldContain("-detailed-exitcode");
        command.ThrowsOnError.ShouldBeFalse();
    }

    [Fact]
    public async Task Plan_StillFailsWhenItCouldNotRun()
    {
        _commands.Respond(c => c.Arguments.Contains("plan"), new CommandResult(1, "", "no valid credential sources"));

        await Should.ThrowAsync<CommandFailedException>(
            async () => await Tofu().Plan(TestContext.Current.CancellationToken));
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

    private OpenTofuClient Tofu() => new(_commands, Options.Create(_options));
}
