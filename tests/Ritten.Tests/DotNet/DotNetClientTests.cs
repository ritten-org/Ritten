using NuGet.Versioning;
using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Reporting;
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

        // The mechanism is what this pins — one evaluation of the project file, every property
        // asked for in that one pass. Which properties reach the Project is the next tests' business.
        var arguments = _commands.Executed.ShouldHaveSingleItem().Arguments;
        arguments.Take(2).ShouldBe(["msbuild", "/repo/src/My.Package.csproj"]);
        arguments.Skip(2).ShouldAllBe(a => a.StartsWith("-getProperty:"));
        arguments.ShouldContain("-getProperty:PackageId");
        arguments.ShouldContain("-getProperty:Version");
        project.IsSuccess.ShouldBeTrue();
        project.Value.Name.ShouldBe("My.Package");
        project.Value.Version.ShouldBe(NuGetVersion.Parse("1.2.3-beta.1"));
    }

    [Fact]
    public async Task ReadProject_ReadsToolAndPackageMetadata()
    {
        _commands.Respond(
            c => c.Arguments.Contains("msbuild"),
            new CommandResult(
                0,
                """
                {"Properties":{"PackageId":"My.Tool","Version":"1.2.3","PackAsTool":"true","ToolCommandName":"mytool",
                "Description":"A tool.","PackageReadmeFile":"README.md","PackageLicenseExpression":"MIT","PackageLicenseFile":""}}
                """,
                ""));

        var project = await _client.ReadProject(ProjectFile("/repo/src/My.Tool.csproj"), TestContext.Current.CancellationToken);

        project.IsSuccess.ShouldBeTrue();
        project.Value.IsTool.ShouldBeTrue();
        project.Value.ToolCommand.ShouldBe("mytool");
        project.Value.Metadata.Description.ShouldBe("A tool.");
        project.Value.Metadata.HasDescription.ShouldBeTrue();
        project.Value.Metadata.HasReadme.ShouldBeTrue();
        project.Value.Metadata.HasLicense.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadProject_ReadsALibraryAsBareMetadata()
    {
        // MSBuild reports unset properties as empty strings, and the SDK substitutes a
        // placeholder description; neither counts as metadata.
        _commands.Respond(
            c => c.Arguments.Contains("msbuild"),
            new CommandResult(
                0,
                """
                {"Properties":{"PackageId":"My.Package","Version":"1.2.3","PackAsTool":"","ToolCommandName":"",
                "Description":"Package Description","PackageReadmeFile":"","PackageLicenseExpression":"","PackageLicenseFile":""}}
                """,
                ""));

        var project = await _client.ReadProject(ProjectFile("/repo/src/My.Package.csproj"), TestContext.Current.CancellationToken);

        project.IsSuccess.ShouldBeTrue();
        project.Value.IsTool.ShouldBeFalse();
        project.Value.ToolCommand.ShouldBeNull();
        project.Value.Metadata.HasDescription.ShouldBeFalse();
        project.Value.Metadata.HasReadme.ShouldBeFalse();
        project.Value.Metadata.HasLicense.ShouldBeFalse();
    }

    [Fact]
    public async Task InstalledToolVersion_FindsTheToolCaseInsensitively()
    {
        // `dotnet tool list` prints package ids lowercased.
        _commands.Respond(
            c => c.Arguments.Contains("list"),
            new CommandResult(0, "Package Id      Version      Commands\n----------------------------------------\nmy.tool         1.2.3        mytool\nother           2.0.0        other\n", ""));

        var version = await _client.InstalledToolVersion("My.Tool", ToolScope.Global, TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments.ShouldBe(["tool", "list", "--global"]);
        version.ShouldBe(NuGetVersion.Parse("1.2.3"));
    }

    [Fact]
    public async Task InstalledToolVersion_ReturnsNullWhenTheToolIsNotInstalled()
    {
        _commands.Respond(
            c => c.Arguments.Contains("list"),
            new CommandResult(0, "Package Id      Version      Commands\n----------------------------------------\nother           2.0.0        other\n", ""));

        var version = await _client.InstalledToolVersion("My.Tool", ToolScope.Global, TestContext.Current.CancellationToken);

        version.ShouldBeNull();
    }

    [Fact]
    public async Task ToolInstall_ReplacesTheFeedsWithTheSource()
    {
        // --source, not --add-source: a published package with the same version must not
        // shadow the build being installed.
        var source = Substitute.For<IDirectory>();
        source.AbsolutePath.Returns("/repo/artifacts");

        await _client.ToolInstall(
            new ToolInstallArgs { PackageId = "My.Tool", Scope = ToolScope.Global, Version = NuGetVersion.Parse("1.2.3"), Source = source },
            TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments
            .ShouldBe(["tool", "install", "My.Tool", "--global", "--version", "1.2.3", "--source", "/repo/artifacts"]);
    }

    [Fact]
    public async Task InstalledToolVersion_ReadsThePinFromTheManifestGoverningTheDirectory()
    {
        _commands.Respond(
            c => c.Arguments.Contains("list"),
            new CommandResult(0, "Package Id      Version      Commands      Manifest\n------------------------------------------------\nritten          0.9.0        ritten        /repo/.config/dotnet-tools.json\n", ""));

        var version = await _client.InstalledToolVersion("ritten", ToolScope.Local(In("/repo/services/api")), TestContext.Current.CancellationToken);

        // The scope is the flag the SDK takes and the directory it resolves the manifest from.
        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.ShouldBe(["tool", "list", "--local"]);
        command.WorkingDirectory.ShouldBe("/repo/services/api");
        version.ShouldBe(NuGetVersion.Parse("0.9.0"));
    }

    [Fact]
    public async Task InstalledToolVersion_AnswersNothingForADirectoryNoManifestGoverns()
    {
        _commands.Respond(c => c.Arguments.Contains("list"), new CommandResult(1, "", "Cannot find a manifest file."));

        var version = await _client.InstalledToolVersion("ritten", ToolScope.Local(In("/elsewhere")), TestContext.Current.CancellationToken);

        version.ShouldBeNull();
    }

    [Fact]
    public async Task CreateToolManifest_AsksTheSdkForOneWhereRepositoriesKeepIt()
    {
        // The manifest's schema is the SDK's, so the SDK writes it — this only says where.
        await _client.CreateToolManifest(In("/repo"), TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.ShouldBe(["new", "tool-manifest", "--output", ".config"]);
        command.WorkingDirectory.ShouldBe("/repo");
    }

    [Fact]
    public async Task ToolInstall_PinsATheManifestsTool()
    {
        await _client.ToolInstall(Pin("/repo"), TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.ShouldBe(["tool", "install", "ritten", "--local", "--version", "1.2.3"]);
        command.WorkingDirectory.ShouldBe("/repo");
    }

    [Fact]
    public async Task ToolUpdate_MovesAPinTheManifestAlreadyHas()
    {
        await _client.ToolUpdate(Pin("/repo"), TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments.ShouldBe(["tool", "update", "ritten", "--local", "--version", "1.2.3"]);
    }

    [Fact]
    public async Task ToolInstall_AsksTheFeedForTheLatestWhenNoVersionIsNamed()
    {
        // Both options are the SDK's to default, so an argument nobody set isn't passed.
        await _client.ToolInstall(new ToolInstallArgs { PackageId = "ritten", Scope = ToolScope.Global }, TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments.ShouldBe(["tool", "install", "ritten", "--global"]);
    }

    private static ToolInstallArgs Pin(string directory) =>
        new() { PackageId = "ritten", Scope = ToolScope.Local(In(directory)), Version = NuGetVersion.Parse("1.2.3") };

    private static IDirectory In(string path)
    {
        var directory = Substitute.For<IDirectory>();
        directory.AbsolutePath.Returns(path);
        return directory;
    }

    [Fact]
    public async Task ToolUninstall_RemovesTheGlobalTool()
    {
        await _client.ToolUninstall("My.Tool", ToolScope.Global, TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments.ShouldBe(["tool", "uninstall", "My.Tool", "--global"]);
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
        var client = new DotNetClient(new CommandRunner(Substitute.For<IWorkflowLog>(), fileSystem), fileSystem);

        var result = await client.ReadProject(ProjectFile(project.CsprojPath), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Inherited.Package");
        result.Value.Version.ShouldBe(NuGetVersion.Parse("2.3.4"));
    }

    [Fact]
    public async Task Test_NamesTheProjectWithTheMtpOption()
    {
        // The MTP mode of `dotnet test` rejects a bare positional path.
        _commands.Respond(c => c.Arguments.Contains("test"), new CommandResult(0, "", ""));

        await _client.Test(
            new TestArgs { Project = "tests/My.Tests.csproj", ResultsDirectory = ResultsDirectory() },
            TestContext.Current.CancellationToken);

        var command = _commands.Executed.ShouldHaveSingleItem();
        command.Arguments.Take(3).ShouldBe(["test", "--project", "tests/My.Tests.csproj"]);
    }

    [Fact]
    public async Task Test_ReportsTheOutputTailWhenTheRunFailsWithoutResults()
    {
        // A run that dies before any test reports leaves no TRX behind, so the command's own
        // output is the only diagnosis there is.
        _commands.Respond(
            c => c.Arguments.Contains("test"),
            new CommandResult(5, "error: unknown option: --report-trx\n", ""));

        var result = await _client.Test(
            new TestArgs { ResultsDirectory = ResultsDirectory() },
            TestContext.Current.CancellationToken);

        result.Succeeded.ShouldBeFalse();
        result.FailureOutput.ShouldBe(["error: unknown option: --report-trx"]);
    }

    private static IDirectory ResultsDirectory()
    {
        var directory = Substitute.For<IDirectory>();
        directory.GetFiles("*.trx").Returns([]);
        return directory;
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
