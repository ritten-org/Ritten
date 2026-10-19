namespace Ritten.Reporting.Retirement;

/// <summary>
/// The arguments of <see cref="RetireReportsJob"/>.
/// </summary>
public sealed record RetireReportsArguments
{
    /// <summary>
    /// The subjects reported on this time.
    /// </summary>
    public required ReportSubjects Reported { get; init; }
}
