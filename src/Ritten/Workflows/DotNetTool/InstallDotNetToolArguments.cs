using Ritten.DotNet;
using Ritten.Engine.Workflows;

namespace Ritten.Workflows.DotNetTool;

/// <summary>
/// What installing a tool from the working tree runs with: the project file's arguments, and whether to reinstall.
/// </summary>
public sealed record InstallDotNetToolArguments : DotNetToolArguments
{
    /// <summary>
    /// Whether to reinstall even when the installed version already matches the one just built.
    /// </summary>
    [CommandLineFlag(ForceReinstall.OptionName, "Reinstall even when the installed version already matches the one just built.")]
    public ForceReinstall Reinstall { get; init; } = new(false);
}