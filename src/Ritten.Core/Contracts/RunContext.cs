namespace Ritten.Contracts;

/// <summary>
/// What the active runtime knows about the current run.
/// </summary>
public sealed record RunContext
{
    /// <summary>
    /// The name the run is reported under, e.g. the CI workflow's name.
    /// </summary>
    public string Title { get; init; } = "Workflow";

    /// <summary>
    /// The CI system's identifier for the run, when there is one.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Where the CI system shows the run, when there is one.
    /// </summary>
    public string? Url { get; init; }
}
