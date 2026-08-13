using System.Collections.ObjectModel;
using Ritten.Contracts;

namespace Ritten.Core;

/// <summary>
/// Represents the summary of a pipeline execution, including the exit code and results of each step.
/// </summary>
public class PipelineExecutionSummary
{
    /// <summary>
    /// Creates a new instance of <see cref="PipelineExecutionSummary"/>.
    /// </summary>
    public PipelineExecutionSummary(IPipelineContext context, IEnumerable<StepExecutionSummary> steps, CancellationToken cancellationToken)
    {
        var stepList = steps.ToList().AsReadOnly();
        ExitCode = CalculateExitCode(context, stepList, cancellationToken);
        Steps = stepList;
    }

    /// <summary>
    /// Gets the exit code of the overall pipeline execution.
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// Gets the results of each step in the pipeline execution.
    /// </summary>
    public IReadOnlyCollection<StepExecutionSummary> Steps { get; init; }

    private static int CalculateExitCode(IPipelineContext context, ReadOnlyCollection<StepExecutionSummary> steps, CancellationToken cancellationToken)
    {
        var autoCode = steps.LastOrDefault()?.Result.ExitCode ?? PipelineExitCodes.Success;
        if (steps.Count == 0 && cancellationToken.IsCancellationRequested)
        {
            autoCode = PipelineExitCodes.StoppedAfterCancel;
        }

        if (autoCode != PipelineExitCodes.Success)
        {
            return autoCode;
        }

        return context.ExitCode ?? PipelineExitCodes.Success;
    }
}
