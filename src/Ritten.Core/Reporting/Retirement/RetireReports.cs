using Ritten.Contracts;

namespace Ritten.Reporting.Retirement;

/// <summary>
/// Removes the workflow's subject reports that were not reported on this time.
/// </summary>
/// <param name="destinations">Where reports are published.</param>
/// <param name="context">The run's context, naming the workflow.</param>
/// <param name="log">The run's log.</param>
[Step("retire reports", StepKind.Work)]
public class RetireReports(IEnumerable<IPublishedReports> destinations, RunContext context, IWorkflowLog log)
{
    /// <summary>
    /// Removes every report of this workflow whose subject is not in <paramref name="reported"/>.
    /// </summary>
    public async Task<StepResult> Run(ReportSubjects reported, CancellationToken cancellationToken = default)
    {
        var workflow = new ReportKey(context.Title);
        var kept = reported.Values.Select(subject => new ReportKey(context.Title, subject)).ToHashSet();
        foreach (var destination in destinations)
        {
            var stale = (await destination.List(cancellationToken))
                .Where(key => key.Workflow == workflow.Workflow && key.Subject is not null && !kept.Contains(key));
            foreach (var key in stale)
            {
                log.Detail($"Retiring the report for {key.Subject}.");
                await destination.Delete(key, cancellationToken);
            }
        }

        return StepResult.Successful;
    }
}
