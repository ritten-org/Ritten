using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.NSchema.Steps;

/// <summary>
/// Makes the database what the schema declares.
/// </summary>
/// <param name="nschema">The NSchema client.</param>
/// <param name="report">The build report.</param>
[Step("nschema apply", StepKind.Publish)]
public class NSchemaApply(INSchema nschema, IWorkflowReport report)
{
    /// <summary>
    /// Applies the project.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="environment">The environment an earlier step resolved, when one did.</param>
    /// <param name="settings">How the apply treats the lock and destructive changes, when the job says.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(NSchemaProject project, NSchemaEnvironment? environment, NSchemaApplySettings? settings,
        CancellationToken cancellationToken = default)
    {
        await nschema.Apply(project, environment, settings, cancellationToken);
        report.Section(SectionName.Database).Success("Applied.");
        return StepResult.Successful;
    }
}
