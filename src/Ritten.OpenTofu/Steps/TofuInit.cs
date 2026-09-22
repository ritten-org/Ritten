using Ritten.Contracts;

namespace Ritten.OpenTofu.Steps;

/// <summary>
/// Prepares the root module, so the steps after it have providers and a backend.
/// </summary>
/// <param name="tofu">The OpenTofu client.</param>
[Step("tofu init", StepKind.Work)]
public class TofuInit(IOpenTofu tofu)
{
    /// <summary>
    /// Initialises the root module.
    /// </summary>
    /// <param name="environment">The environment an earlier step resolved, when one did.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(TofuEnvironment? environment, CancellationToken cancellationToken = default)
    {
        await tofu.Init(environment, cancellationToken);
        return StepResult.Successful;
    }
}
