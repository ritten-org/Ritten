namespace Ritten.Contracts;

/// <summary>
/// What a step's outcome means, classified for display and for job-shape rules.
/// </summary>
public enum StepKind
{
    /// <summary>
    /// Does the job's labour — compiling, testing, packing. Failure means the build broke.
    /// </summary>
    Work,

    /// <summary>
    /// A read-only policy check. Failure means the repository needs fixing, not the build.
    /// </summary>
    Validation,

    /// <summary>
    /// May end the job early, in either direction: declined, or nothing left to do.
    /// </summary>
    Gate,

    /// <summary>
    /// Irreversible and outward-facing — tagging, releasing, pushing. The steps dry runs stand in for.
    /// </summary>
    Publish
}
