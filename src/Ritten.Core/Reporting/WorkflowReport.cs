using Ritten.Engine.Runs;

namespace Ritten.Reporting;

/// <summary>
/// The finished run's report.
/// </summary>
/// <param name="Title">The name the run is reported under.</param>
/// <param name="Succeeded">Whether the run succeeded.</param>
/// <param name="Sections">The sections the steps authored.</param>
/// <param name="StoppedAt">The step that ended the run early with nothing to do, or null.</param>
/// <param name="Failure">The failing step and its result, for renderings whose sections don't already explain the failure.</param>
public sealed record WorkflowReport(string Title, bool Succeeded, IReadOnlyList<ReportSection> Sections, StepOutcome? Failure = null, StepOutcome? StoppedAt = null)
{
    /// <summary>
    /// Whether the run ended early with nothing to do and nothing to say: no section was written
    /// before a step stopped it. A sink that speaks to a person may reasonably keep quiet.
    /// </summary>
    public bool IsSilent => StoppedAt is not null && Sections.Count == 0;
}
