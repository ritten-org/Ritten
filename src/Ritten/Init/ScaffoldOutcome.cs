namespace Ritten.Init;

/// <summary>
/// What became of one scaffolded file.
/// </summary>
public enum ScaffoldOutcome
{
    /// <summary>
    /// The file wasn't there, so it was written.
    /// </summary>
    Written,

    /// <summary>
    /// The file had drifted from what Ritten generates, so it was written again.
    /// </summary>
    Rewritten,

    /// <summary>
    /// Nothing to do: the file is the repository's, or it already says what it should.
    /// </summary>
    Matches,

    /// <summary>
    /// The file is Ritten's to generate and says something else
    /// .</summary>
    Differs
}
