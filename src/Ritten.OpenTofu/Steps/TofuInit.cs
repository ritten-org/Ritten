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
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        await tofu.Init(cancellationToken);
        return StepResult.Successful;
    }
}
