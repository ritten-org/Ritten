using System.Text.Json;
using Ritten.Engine;
using Ritten.Engine.Runs;
using Ritten.Engine.Runtimes;
using Ritten.Engine.Workflows;
using Ritten.Reporting;
using Spectre.Console;

namespace Ritten.Tests.Engine.Helpers;

internal static class WorkflowRunBuilderHelpers
{
    /// <summary>
    /// An environment with everything set. Tests supply their own rather than reading the process
    /// environment, which on a GitHub Actions runner is anything but empty.
    /// </summary>
    public static Func<string, string?> Complete { get; } = _ => "set";

    /// <summary>An environment with nothing set.</summary>
    public static Func<string, string?> Empty { get; } = _ => null;

    public static WorkflowRunBuilder Create(
        string workflowName = "Test",
        Func<string, string?>? environment = null,
        bool dryRun = false,
        IWorkflowLog? log = null,
        string settings = "{}",
        RuntimeRegistry? runtimes = null,
        string fileName = RittenProject.DefaultFileName)
    {
        var project = new RittenProject
        {
            Directory = Path.GetTempPath(),
            FileName = fileName,
            Settings = JsonSerializer.Deserialize<JsonElement>(settings)
        };

        var builder = new WorkflowRunBuilder(
                project,
                (runtimes ?? new RuntimeRegistry()).Detect(environment ?? Complete).Value.ShouldNotBeNull(),
                new SpectreWorkflowConsole(AnsiConsole.Console, WorkflowLogLevel.Detail))
            .WithWorkflow(new WorkflowSelection(new Support.TestWorkflow(workflowName, label: workflowName), project))
            .WithDryRun(dryRun);
        return log is null ? builder : builder.WithLog(log);
    }
}
