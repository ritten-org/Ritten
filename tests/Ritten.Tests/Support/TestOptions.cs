using Ritten.Changelogs;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.NuGet;
using Ritten.Releases;

namespace Ritten.Tests.Support;

/// <summary>
/// Preconfigured options, and the arguments a job would hand its steps.
/// </summary>
public static class TestOptions
{
    public static DotNetBuildSettings Build() => new() { Projects = ["src/My.Package/My.Package.csproj"] };

    public static ChangelogSettings Changelog() => new();

    public static ReleaseSettings Release() => new();

    public static NuGetOptions NuGet() => new() { ApiKey = "test-api-key" };

    public static GitOptions Git() => new();
}
