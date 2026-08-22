using Spectre.Console;

namespace Ritten.Reporting;

/// <summary>
/// The console and prompt for work that happens outside a run.
/// </summary>
public static class EngineConsole
{
    /// <summary>
    /// Creates a console that narrates at the given level.
    /// </summary>
    /// <param name="minimumLogLevel">The lowest level of message to print.</param>
    public static IWorkflowConsole Create(WorkflowLogLevel minimumLogLevel = WorkflowLogLevel.Detail) =>
        new SpectreWorkflowConsole(AnsiConsole.Console, minimumLogLevel);

    /// <summary>
    /// Creates a prompt that asks at the terminal.
    /// </summary>
    public static IWorkflowPrompt Prompt() => new ConsolePrompt(AnsiConsole.Console);
}
