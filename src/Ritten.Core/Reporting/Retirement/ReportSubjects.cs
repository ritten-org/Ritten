namespace Ritten.Reporting.Retirement;

/// <summary>
/// The subjects reported on this time, whose reports stay.
/// </summary>
/// <param name="Values">The subjects, as given to <see cref="Engine.RunOptions.Subject"/>.</param>
public sealed record ReportSubjects(IReadOnlyCollection<string> Values);
