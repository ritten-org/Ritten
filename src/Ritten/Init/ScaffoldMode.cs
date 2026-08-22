namespace Ritten.Init;

/// <summary>
/// How far the scaffolder is allowed to go.
/// </summary>
public enum ScaffoldMode
{
    /// <summary>
    /// Write what's missing, and leave anything already there alone.
    /// </summary>
    Write,

    /// <summary>
    /// Write what's missing, and bring the files Ritten generates back to what it generates.
    /// Seeds are still left alone: a changelog belongs to the repository the moment it exists.
    /// </summary>
    Rewrite,

    /// <summary>
    /// Write nothing, and report what would change.
    /// </summary>
    Check
}
