using Ritten.Contracts;
using Ritten.Engine.Runs;
using Ritten.Reporting;

namespace Ritten.Tests.Support;

/// <summary>
/// A console narrative that keeps what it was told, so a test can read back what a run printed.
/// </summary>
internal sealed class RecordingConsole : IWorkflowConsole
{
    public List<(WorkflowLogLevel Level, string? Message)> Entries { get; } = [];

    /// <summary>
    /// The errors printed, in order.
    /// </summary>
    public IEnumerable<string?> Errors => Entries.Where(e => e.Level == WorkflowLogLevel.Error).Select(e => e.Message);

    public bool IsEnabled(WorkflowLogLevel level) => true;

    public void Log(WorkflowLogLevel level, string? message, Exception? exception = null) => Entries.Add((level, message));

    public Task OnWorkflowStarted(WorkflowJob job, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task OnStepStarted(Step step, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task OnStepCompleted(Step step, StepResult result, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task OnWorkflowCompleted(WorkflowResult result, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
