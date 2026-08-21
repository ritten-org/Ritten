using Ritten.Engine.Workflows;

namespace Ritten.DotNet;

/// <summary>
/// What a tool job can be asked for.
/// </summary>
public static class ToolArguments
{
    /// <summary>
    /// Reinstalls a tool whose installed version already matches the one just built.
    /// </summary>
    public static JobArgument<bool> Reinstall { get; } = JobArgument.Value<bool>(
        "force",
        "Reinstall even when the installed version already matches the one just built."
    );
}
