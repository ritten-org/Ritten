using Ritten.Contracts;
using Ritten.Engine.Workflows;

namespace Ritten.Reporting.Retirement;

/// <summary>
/// Removes the workflow's reports for subjects it no longer reports on, after a host has run a job for each subject.
/// </summary>
public sealed class RetireReportsJob : Job<RetireReportsArguments>
{
    /// <inheritdoc />
    public override string Name => "retire-reports";

    /// <inheritdoc />
    public override string Description => "Removes the reports of subjects that were not reported on this time.";

    /// <inheritdoc />
    public override JobKind Kind => JobKind.Work;

    /// <inheritdoc />
    public override bool RequiresProject => false;

    /// <inheritdoc />
    public override bool Reports => false;

    /// <inheritdoc />
    public override IReadOnlyList<Step> Steps { get; } = [Step.FromType<RetireReports>()];
}
