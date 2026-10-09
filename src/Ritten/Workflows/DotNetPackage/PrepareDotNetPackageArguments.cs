using Ritten.Engine.Workflows;
using Ritten.Releases;

namespace Ritten.Workflows.DotNetPackage;

/// <summary>
/// What preparing a package's next release runs with: the project file's arguments, and the version the caller names.
/// </summary>
public sealed record PrepareDotNetPackageArguments : DotNetPackageArguments
{
    /// <summary>
    /// The version to prepare, in place of the one derived from the changelog.
    /// </summary>
    [CommandLineOption(RequestedVersion.OptionName, "The version to prepare, in place of the one derived from the changelog.")]
    public RequestedVersion Version { get; init; } = RequestedVersion.None;
}