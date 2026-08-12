using Wolfe.Hamelin.Build.Models;

namespace Wolfe.Hamelin.Build.Tests.Support;

public static class TestOptions
{
    /// <summary>
    /// A fully-populated <see cref="BuildOptions"/> matching the defaults in appsettings.json.
    /// </summary>
    public static BuildOptions Build() => new()
    {
        ArtifactsDirectory = "artifacts",
        TempDirectory = "temp",
        Configuration = "Release",
        ProjectFile = "src/My.Package/My.Package.csproj",
        ChangelogFile = "CHANGELOG.md",
        NuGetFeed = "https://api.nuget.org/v3/index.json",
        NuGetApiKey = "test-api-key"
    };
}
