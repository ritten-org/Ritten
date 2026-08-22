namespace Ritten.Init;

/// <summary>
/// What became of one scaffolded file.
/// </summary>
public enum ScaffoldOutcome
{
    /// <summary>
    /// The file was written.
    /// </summary>
    Written,

    /// <summary>
    /// The file was already there and says what it should, so it was left alone.
    /// </summary>
    Matches,

    /// <summary>
    /// The file was already there and says something else, so it was left alone.
    /// </summary>
    Differs
}
