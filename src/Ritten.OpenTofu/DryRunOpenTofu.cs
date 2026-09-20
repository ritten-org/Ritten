using Ritten.Reporting;

namespace Ritten.OpenTofu;

/// <summary>
/// Skips the apply; everything else passes through, because the plan is the rehearsal.
/// </summary>
internal sealed class DryRunOpenTofu(IWorkflowLog log, IOpenTofu inner) : IOpenTofu
{
    /// <inheritdoc />
    public Task Init(CancellationToken ct = default) => inner.Init(ct);

    /// <inheritdoc />
    public Task<TofuPlanResult> Plan(CancellationToken ct = default) => inner.Plan(ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<string>?> VerifyFormatting(CancellationToken ct = default) => inner.VerifyFormatting(ct);

    /// <inheritdoc />
    public Task Apply(CancellationToken ct = default)
    {
        log.Skipped("Would apply the plan.");
        return Task.CompletedTask;
    }
}
