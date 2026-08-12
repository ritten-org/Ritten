using NuGet.Versioning;

namespace Wolfe.Hamelin.Build.Models;

public class ChangelogEntry
{
    public required NuGetVersion Version { get; init; }
    public required string Body { get; init; }
}
