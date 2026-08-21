using Ritten.Engine.Workflows;

namespace Ritten.DotNet;

/// <summary>
/// What a tool job can be asked for.
/// </summary>
public static class ToolInputs
{
    /// <summary>
    /// Reinstalls a tool whose installed version already matches the one just built.
    /// </summary>
    public static FlagArgument Reinstall { get; } = JobArgument.Flag(
        "force",
        "Reinstall even when the installed version already matches the one just built."
    );
}
