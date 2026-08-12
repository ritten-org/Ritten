using Wolfe.Hamelin.Build.Models;

namespace Wolfe.Hamelin.Build.Tests.Support;

public static class TestOptions
{
    public static BuildOptions Build() => new() { ProjectFile = "src/My.Package/My.Package.csproj" };

    public static ChangelogOptions Changelog() => new();

    public static NuGetOptions NuGet() => new() { ApiKey = "test-api-key" };

    public static ReleaseOptions Release() => new();
}
