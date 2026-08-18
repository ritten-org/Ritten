using Ritten.Changelogs;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.NuGet;

namespace Ritten.Tests.Support;

public static class TestOptions
{
    public static DotNetOptions DotNet() => new()
    {
        ProjectFile = "src/My.Package/My.Package.csproj",
        Projects = ["src/My.Package/My.Package.csproj"]
    };

    public static ChangelogOptions Changelog() => new();

    public static NuGetOptions NuGet() => new() { ApiKey = "test-api-key" };

    public static GitOptions Git() => new();
}
