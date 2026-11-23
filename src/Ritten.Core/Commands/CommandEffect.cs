namespace Ritten.Commands;

/// <summary>
/// Describes how a command will affect its environment. Used to determine whether it gets run on a dry run.
/// </summary>
public enum CommandEffect
{
    /// <summary>
    /// It changes nothing.
    /// </summary>
    None,

    /// <summary>
    /// It changes something.
    /// </summary>
    Change
}
