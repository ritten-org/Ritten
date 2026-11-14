using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.NSchema.Steps;

/// <summary>
/// Works out what the change would do to the database, and puts it in the report.
/// </summary>
/// <remarks>
/// Stops the job when there is nothing to apply, as a plan with no changes has answered the
/// question the job was asking.
/// </remarks>
/// <param name="nschema">The NSchema client.</param>
/// <param name="report">The build report.</param>
/// <param name="log">The run's log.</param>
[Step("nschema plan", StepKind.Check)]
public class NSchemaPlan(INSchema nschema, IWorkflowReport report, IWorkflowLog log)
{
    /// <summary>
    /// Plans the project.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="environment">The environment an earlier step resolved, when one did.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The plan, for a step after this one to judge; nothing to do when it is empty.</returns>
    public async Task<StepResult<NSchemaPlanResult>> Run(NSchemaProject project, NSchemaEnvironment? environment, CancellationToken cancellationToken = default)
    {
        var plan = await nschema.Plan(project, environment, cancellationToken);
        if (!plan.HasChanges)
        {
            report.Section(SectionName.Database).Success("No changes. The database matches the schema.");
            log.Status("Nothing to apply.");
            return StepResult.NothingToDo;
        }

        report.Section(SectionName.Database).Note(plan.Output);
        return plan;
    }
}
