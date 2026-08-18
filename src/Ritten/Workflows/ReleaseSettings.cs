using Ritten.Releases;

namespace Ritten.Workflows;

/// <summary>
/// The <c>release</c> section of <c>ritten.json</c> for .NET projects.
/// </summary>
public sealed record ReleaseSettings
{
    /// <summary>
    /// The prefix for release tag names, so that tags are <c>TagPrefix + version</c>, e.g. <c>v1.2.0</c>.
    /// Also used when validating the changelog's compare links, so tags and links can't drift apart.
    /// </summary>
    public string TagPrefix { get; init; } = "v";

    /// <summary>
    /// The V3 index URL of the NuGet feed the package is validated against and published to.
    /// </summary>
    public string Feed { get; init; } = "https://api.nuget.org/v3/index.json";

    /// <summary>
    /// How published versions are grouped into release lines:
    /// <c>major</c> (the default) allows publishing fixes to an older major version,
    /// and <c>minor</c> also allows older minors, for projects that treat the major number as a product version.
    /// </summary>
    public ReleaseLine Lines { get; init; } = ReleaseLine.Major;
}
