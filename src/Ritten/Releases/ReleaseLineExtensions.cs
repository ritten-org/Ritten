using NuGet.Versioning;

namespace Ritten.Releases;

/// <summary>
/// How a <see cref="ReleaseLine"/> scope groups versions.
/// </summary>
public static class ReleaseLineExtensions
{
    extension(ReleaseLine lines)
    {
        /// <summary>
        /// Whether two versions belong to the same release line under this scope.
        /// </summary>
        /// <param name="a">The first version.</param>
        /// <param name="b">The second version.</param>
        public bool SameLine(NuGetVersion a, NuGetVersion b) =>
            a.Major == b.Major && (lines == ReleaseLine.Major || a.Minor == b.Minor);

        /// <summary>
        /// The display label of the given version's line, e.g. <c>1.x</c> or <c>1.2.x</c>.
        /// </summary>
        /// <param name="version">The version whose line is being named.</param>
        public string Label(NuGetVersion version) =>
            lines == ReleaseLine.Major ? $"{version.Major}.x" : $"{version.Major}.{version.Minor}.x";
    }
}
