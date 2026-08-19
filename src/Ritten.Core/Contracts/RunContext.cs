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
}
