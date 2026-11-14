using Ritten.Reporting;

namespace Ritten.NSchema;

/// <summary>
/// Skips the apply; everything else passes through, because the plan is the rehearsal.
/// </summary>
internal sealed class DryRunNSchema(IWorkflowLog log, INSchema inner) : INSchema
{
    /// <inheritdoc />
    public Task<IReadOnlyList<string>?> VerifyFormatting(NSchemaProject project, CancellationToken ct = default) => inner.VerifyFormatting(project, ct);

    /// <inheritdoc />
    public Task<NSchemaPlanResult> Plan(NSchemaProject project, NSchemaEnvironment? environment = null, CancellationToken ct = default) =>
        inner.Plan(project, environment, ct);

    /// <inheritdoc />
    public Task<NSchemaApplyResult> Apply(NSchemaProject project, NSchemaEnvironment? environment = null, NSchemaApplySettings? settings = null,
        CancellationToken ct = default)
    {
        log.Skipped("Would apply the schema.");
        return Task.FromResult(new NSchemaApplyResult(""));
    }
}
