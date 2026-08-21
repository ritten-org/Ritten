using NuGet.Versioning;
using Ritten.Engine;
using Ritten.Engine.Workflows;

namespace Ritten.Releases;

/// <summary>
/// What a release job can be asked for. The job declares these and reads them back through the
/// same instances, so nothing is spelled twice.
/// </summary>
public static class ReleaseArguments
{
    /// <summary>
    /// The version to prepare, in place of the one derived from the changelog.
    /// </summary>
    public static JobArgument<NuGetVersion> Version { get; } = JobArgument.Value(
        "version",
        "The version to prepare, in place of the one derived from the changelog.",
        text => NuGetVersion.TryParse(text, out var version)
            ? new Result<NuGetVersion>(version)
            : Result.Error($"'{text}' is not a version. Give one like 1.2.0."));
}
