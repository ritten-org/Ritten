namespace Ritten.NSchema;

/// <summary>
/// Describes what NSchema does with a change that would destroy data (<c>--destructive-actions</c>).
/// </summary>
public enum NSchemaDestructiveActions
{
    /// <summary>
    /// The plan fails.
    /// </summary>
    Error,

    /// <summary>
    /// The plan warns and goes ahead.
    /// </summary>
    Warn,

    /// <summary>
    /// The plan goes ahead.
    /// </summary>
    Allow,

    /// <summary>
    /// The change is left out of the plan.
    /// </summary>
    Ignore
}
