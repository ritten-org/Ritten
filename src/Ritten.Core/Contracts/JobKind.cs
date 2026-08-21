namespace Ritten.Contracts;

/// <summary>
/// What a job is for.
/// </summary>
public enum JobKind
{
    /// <summary>
    /// Basic labour. The job is run when someone asks for it.
    /// </summary>
    Work,

    /// <summary>
    /// Judges whether a change could ship, without shipping it. The gate a pull request passes.
    /// </summary>
    Check,

    /// <summary>
    /// Publishes a release, so it runs deliberately rather than on every change.
    /// </summary>
    Publish
}
