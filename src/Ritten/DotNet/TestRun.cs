namespace Ritten.DotNet;

/// <summary>
/// The outcome of a test run, parsed from a TRX results file.
/// </summary>
public record TestRun
{
    /// <summary>
    /// The number of tests that passed.
    /// </summary>
    public required int Passed { get; init; }

    /// <summary>
    /// The number of tests that failed.
    /// </summary>
    public required int Failed { get; init; }

    /// <summary>
    /// The number of tests that were skipped.
    /// </summary>
    public required int Skipped { get; init; }

    /// <summary>
    /// The individual test failures, if any.
    /// </summary>
    public IReadOnlyList<TestFailure> Failures { get; init; } = [];

    /// <summary>
    /// The total number of tests in the run.
    /// </summary>
    public int Total => Passed + Failed + Skipped;
}
