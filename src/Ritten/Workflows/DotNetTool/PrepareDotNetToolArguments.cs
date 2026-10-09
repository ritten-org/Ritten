using Ritten.Engine.Workflows;
using Ritten.Releases;

namespace Ritten.Workflows.DotNetTool;

/// <summary>
/// What preparing a tool's next release runs with: the project file's arguments, and the version the caller names.
/// </summary>
public sealed record PrepareDotNetToolArguments : DotNetToolArguments
{
    /// <summary>
    /// The version to prepare, in place of the one derived from the changelog.
    /// </summary>
    [CommandLineOption(RequestedVersion.OptionName, "The version to prepare, in place of the one derived from the changelog.")]
    public RequestedVersion Version { get; init; } = RequestedVersion.None;
}
