using Hamelin;
using Hamelin.FileSystem;
using Microsoft.Extensions.Logging.Abstractions;
using NuGet.Versioning;
using Ritten.Commands;
using Ritten.DotNet;
using Ritten.Tests.Support;

namespace Ritten.Tests.DotNet;

public class DotNetClientTests
{
    private readonly FakeCommandRunner _commands = new();
    private readonly DotNetClient _client;

    public DotNetClientTests()
    {
        _client = new DotNetClient(_commands, Substitute.For<IPipelineContext>());
    }

    [Fact]
    public async Task ReadProject_EvaluatesTheProjectWithMSBuild()
    {
        _commands.Respond(
            c => c.Arguments.Contains("msbuild"),
            new CommandResult(0, """{"Properties":{"PackageId":"My.Package","Version":"1.2.3-beta.1"}}""", ""));

        var project = await _client.ReadProject(ProjectFile("/repo/src/My.Package.csproj"), TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments
            .ShouldBe(["msbuild", "/repo/src/My.Package.csproj", "-getProperty:PackageId", "-getProperty:Version"]);
        project.Name.ShouldBe("My.Package");
        project.Version.ShouldBe(NuGetVersion.Parse("1.2.3-beta.1"));
    }

    [Fact]
    public async Task ReadProject_ThrowsWhenTheVersionEvaluatesEmpty()
    {
        _commands.Respond(
            c => c.Arguments.Contains("msbuild"),
            new CommandResult(0, """{"Properties":{"PackageId":"My.Package","Version":""}}""", ""));

        var exception = await Should.ThrowAsync<Exception>(
            () => _client.ReadProject(ProjectFile("/repo/src/My.Package.csproj"), TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("Version");
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

        var context = Substitute.For<IPipelineContext>();
        context.CurrentDirectory.Returns(project.Root);
        var client = new DotNetClient(new CommandRunner(NullLogger<CommandRunner>.Instance, context), context);

        var result = await client.ReadProject(ProjectFile(project.CsprojPath), TestContext.Current.CancellationToken);

        result.Name.ShouldBe("Inherited.Package");
        result.Version.ShouldBe(NuGetVersion.Parse("2.3.4"));
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
