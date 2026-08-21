using NuGet.Versioning;

namespace Ritten.Releases;

internal static class NuGetVersionExtensions
{
    extension(NuGetVersion version)
    {
        /// <summary>
        /// Gets the version that should follow the given one for a release of the given kind.
        /// </summary>
        /// <param name="kind">What the release does to what already shipped.</param>
        public NuGetVersion Next(ReleaseKind kind)
        {
            // A prerelease is already the next version — finishing it is the release.
            if (version.IsPrerelease)
            {
                return new NuGetVersion(version.Major, version.Minor, version.Patch);
            }

            // Before 1.0 the major is a statement of intent rather than a compatibility promise, so
            // SemVer's own advice applies: breaking changes ride the minor until the API settles.
            if (kind == ReleaseKind.Breaking)
            {
                return version.Major == 0
                    ? new NuGetVersion(0, version.Minor + 1, 0)
                    : new NuGetVersion(version.Major + 1, 0, 0);
            }

            return kind == ReleaseKind.Features
                ? new NuGetVersion(version.Major, version.Minor + 1, 0)
                : new NuGetVersion(version.Major, version.Minor, version.Patch + 1);
        }
    }
}
