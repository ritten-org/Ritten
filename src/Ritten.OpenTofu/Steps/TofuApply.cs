using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.OpenTofu.Steps;

/// <summary>
/// Applies the configuration to the infrastructure.
/// </summary>
/// <param name="tofu">The OpenTofu client.</param>
/// <param name="report">The build report.</param>
[Step("tofu apply", StepKind.Publish)]
public class TofuApply(IOpenTofu tofu, IWorkflowReport report)
{
    /// <summary>
    /// Applies the root module.
    /// </summary>
    /// <param name="environment">The environment an earlier step resolved, when one did.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(TofuEnvironment? environment, CancellationToken cancellationToken = default)
    {
        await tofu.Apply(environment, cancellationToken);
        report.Section(SectionName.Infrastructure).Success("Applied.");
        return StepResult.Successful;
    }
}
