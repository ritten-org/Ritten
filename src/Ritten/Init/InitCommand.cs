using System.CommandLine;
using Ritten.Engine.Workflows;
using Ritten.Reporting;

namespace Ritten.Init;

/// <summary>
/// The <c>init</c> command, which sets a repository up to run a workflow.
/// </summary>
internal static class InitCommand
{
    public static Command Create(WorkflowRegistry workflows, string projectFile)
    {
        var workflow = new Argument<string?>("workflow")
        {
            Description = "The workflow to scaffold for. Derived from the repository when omitted.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var check = new Option<bool>("--check")
        {
            Description = "Report what's missing or out of date without writing anything."
        };

        var verbose = new Option<bool>("--verbose", "-v") { Description = "Show every log entry in its highest detail." };

        var command = new Command("init", "Sets this repository up to run a Ritten workflow.") { workflow, check, verbose };
        command.SetAction(async (parseResult, ct) =>
        {
            var console = EngineConsole.Create(parseResult.GetValue(verbose) ? WorkflowLogLevel.Verbose : WorkflowLogLevel.Detail);

            var init = new ProjectInitializer(workflows, console, EngineConsole.Prompt(), projectFile);
            var exitCode = await init.Run(
                Environment.CurrentDirectory,
                parseResult.GetValue(workflow),
                parseResult.GetValue(check),
                ct
            );

            return exitCode;
        });

        return command;
    }
}
