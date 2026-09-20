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
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        await tofu.Apply(cancellationToken);
        report.Section(SectionName.Infrastructure).Success("Applied.");
        return StepResult.Successful;
    }
}
