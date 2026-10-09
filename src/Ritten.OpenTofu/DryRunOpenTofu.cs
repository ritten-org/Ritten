using Ritten.Reporting;

namespace Ritten.OpenTofu;

/// <summary>
/// Skips the apply; everything else passes through, because the plan is the rehearsal.
/// </summary>
internal sealed class DryRunOpenTofu(IWorkflowLog log, IOpenTofu inner) : IOpenTofu
{
    /// <inheritdoc />
    public Task Init(TofuModule module, TofuEnvironment? environment = null, CancellationToken ct = default) => inner.Init(module, environment, ct);

    /// <inheritdoc />
    public Task<TofuPlanResult> Plan(TofuModule module, TofuEnvironment? environment = null, CancellationToken ct = default) => inner.Plan(module, environment, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<string>?> VerifyFormatting(TofuModule module, CancellationToken ct = default) => inner.VerifyFormatting(module, ct);

    /// <inheritdoc />
    public Task Apply(TofuModule module, TofuEnvironment? environment = null, CancellationToken ct = default)
    {
        log.Skipped("Would apply the plan.");
        return Task.CompletedTask;
    }
}
