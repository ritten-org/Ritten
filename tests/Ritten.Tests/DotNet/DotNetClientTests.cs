using NuGet.Versioning;
using Ritten.Commands;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Tests.Support;

namespace Ritten.Tests.DotNet;

public class DotNetClientTests
{
    private readonly FakeCommandRunner _commands = new();
    private readonly DotNetClient _client;

    public DotNetClientTests()
    {
        _client = new DotNetClient(_commands, Substitute.For<IFileSystem>());
    }

    [Fact]
    public async Task ReadProject_EvaluatesTheProjectWithMSBuild()
    {
        _commands.Respond(
            c => c.Arguments.Contains("msbuild"),
            new CommandResult(0, """{"Properties":{"PackageId":"My.Package","Version":"1.2.3-beta.1"}}""", ""));

        var project = await _client.ReadProject(ProjectFile("/repo/src/My.Package.csproj"), TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments
            .ShouldBe(["msbuild", "/repo/src/My.Package.csproj", "-getProperty:PackageId", "-getProperty:Version", "-getProperty:RepositoryUrl"]);
        project.IsSuccess.ShouldBeTrue();
        project.Value.Name.ShouldBe("My.Package");
        project.Value.Version.ShouldBe(NuGetVersion.Parse("1.2.3-beta.1"));
    }

    [Fact]
    public async Task ReadProject_ReportsAnEmptyVersion()
    {
        _commands.Respond(
            c => c.Arguments.Contains("msbuild"),
            new CommandResult(0, """{"Properties":{"PackageId":"My.Package","Version":""}}""", ""));

        var project = await _client.ReadProject(ProjectFile("/repo/src/My.Package.csproj"), TestContext.Current.CancellationToken);

        project.IsError.ShouldBeTrue();
        project.Errors.ShouldHaveSingleItem().Message.ShouldContain("does not set a Version");
    }

    [Fact]
    public async Task ReadProject_ReportsEveryMissingPropertyAtOnce()
    {
        // Rather than sending someone round the loop twice.
        _commands.Respond(
            c => c.Arguments.Contains("msbuild"),
            new CommandResult(0, """{"Properties":{"PackageId":"","Version":""}}""", ""));

        var project = await _client.ReadProject(ProjectFile("/repo/src/My.Package.csproj"), TestContext.Current.CancellationToken);

        project.IsError.ShouldBeTrue();
        project.Errors.Select(e => e.Message).ShouldBe([
            "'My.Package.csproj' does not set a PackageId.",
            "'My.Package.csproj' does not set a Version."
        ]);
    }

    [Fact]
    public async Task ReadProject_ResolvesPropertiesInheritedFromDirectoryBuildProps()
    {
        // End to end against the real SDK: the csproj declares neither property; both come
        // from Directory.Build.props, which the old raw-XML parsing couldn't see.
        using var project = new TempProject(
            directoryBuildProps:
            """
            <Project>
              <PropertyGroup>
                <PackageId>Inherited.Package</PackageId>
                <Version>2.3.4</Version>
              </PropertyGroup>
            </Project>
            """,
            csproj:
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.ProjectRoot.AbsolutePath.Returns(project.Root);
        var client = new DotNetClient(new CommandRunner(Substitute.For<IPipelineLog>(), fileSystem), fileSystem);

        var result = await client.ReadProject(ProjectFile(project.CsprojPath), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Inherited.Package");
        result.Value.Version.ShouldBe(NuGetVersion.Parse("2.3.4"));
    }

    private static IFile ProjectFile(string path)
    {
        var file = Substitute.For<IFile>();
        file.AbsolutePath.Returns(path);
        file.Name.Returns(Path.GetFileName(path));
        return file;
    }

    private sealed class TempProject : IDisposable
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("ritten-msbuild-").FullName;

        public string CsprojPath => Path.Combine(Root, "Temp.Project.csproj");

        public TempProject(string directoryBuildProps, string csproj)
        {
            File.WriteAllText(Path.Combine(Root, "Directory.Build.props"), directoryBuildProps);
            File.WriteAllText(CsprojPath, csproj);
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
