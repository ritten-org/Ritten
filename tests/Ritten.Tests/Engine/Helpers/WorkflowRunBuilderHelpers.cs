using System.Text.Json;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Engine.Runtimes;
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
        var builder = new WorkflowRunBuilder(
                new RittenProject { Directory = Path.GetTempPath(), FileName = fileName, Settings = JsonSerializer.Deserialize<JsonElement>(settings) },
                (runtimes ?? new RuntimeRegistry()).Detect(environment ?? Complete).Value.ShouldNotBeNull(),
                new SpectreWorkflowConsole(AnsiConsole.Console, WorkflowLogLevel.Detail))
            .WithWorkflowLabel(workflowName)
            .WithDryRun(dryRun);
        return log is null ? builder : builder.WithLog(log);
    }
}
