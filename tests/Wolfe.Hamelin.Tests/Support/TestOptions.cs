using Wolfe.Hamelin.Pipelines;
using Wolfe.Hamelin.Pipelines.DotNet;
using Wolfe.Hamelin.Pipelines.Git;
using Wolfe.Hamelin.Pipelines.NuGet;

namespace Wolfe.Hamelin.Tests.Support;

public static class TestOptions
{
    public static PipelineOptions Pipeline() => new();

    public static DotNetOptions DotNet() => new() { ProjectFile = "src/My.Package/My.Package.csproj" };

    public static ChangelogOptions Changelog() => new();

    public static NuGetOptions NuGet() => new() { ApiKey = "test-api-key" };

    public static GitOptions Git() => new();
}
