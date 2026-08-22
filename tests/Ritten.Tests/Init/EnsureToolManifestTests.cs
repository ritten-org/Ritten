using NuGet.Versioning;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.Git;
using Ritten.Init;
using Ritten.Init.Steps;
using Ritten.Reporting;

namespace Ritten.Tests.Init;

/// <summary>
/// The manifest's schema is the SDK's, so these tests pin what Ritten asks the SDK for rather
/// than any JSON: a repository that pins other tools keeps them because Ritten never writes the
/// file itself.
/// </summary>
public class EnsureToolManifestTests
{
    private static readonly NuGetVersion Version = NuGetVersion.Parse("1.2.3");

    private readonly IDotNet _dotnet = Substitute.For<IDotNet>();
    private readonly IGit _git = Substitute.For<IGit>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IDirectory _repository = Substitute.For<IDirectory>();
    private readonly IDirectory _project = Substitute.For<IDirectory>();

    public EnsureToolManifestTests()
    {
        _fileSystem.ProjectRoot.Returns(_project);
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns(_repository);
        SetManifest(exists: false);
    }

    [Fact]
    public async Task CreatesAManifestWhenTheRepositoryHasNone()
    {
        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _dotnet.Received().CreateToolManifest(_repository, Arg.Any<CancellationToken>());
        await _dotnet.Received().ToolInstall(Arg.Is<ToolInstallArgs>(a => a.PackageId == "ritten" && a.Version == Version), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(".config/dotnet-tools.json")]
    [InlineData("dotnet-tools.json")]
    public async Task LeavesAManifestTheRepositoryAlreadyHas(string path)
    {
        // Both places are ones the SDK reads, and creating a second manifest beside the first
        // would leave the repository pinning two different sets of tools.
        SetManifest(exists: true, at: path);

        await Step().Run(TestContext.Current.CancellationToken);

        await _dotnet.DidNotReceive().CreateToolManifest(Arg.Any<IDirectory>(), Arg.Any<CancellationToken>());
        await _dotnet.Received().ToolInstall(Arg.Any<ToolInstallArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNothingWhenTheVersionIsAlreadyPinned()
    {
        Pinned(Version);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        await _dotnet.DidNotReceive().ToolInstall(Arg.Any<ToolInstallArgs>(), Arg.Any<CancellationToken>());
        await _dotnet.DidNotReceive().ToolUpdate(Arg.Any<ToolInstallArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MovesAPinThatIsBehind()
    {
        // Install refuses a tool the manifest already pins; update is the SDK's word for moving
        // one, and which of the two it is, is decided here rather than hidden in the client.
        Pinned(NuGetVersion.Parse("1.0.0"));

        await Step().Run(TestContext.Current.CancellationToken);

        await _dotnet.Received().ToolUpdate(Arg.Is<ToolInstallArgs>(a => a.Version == Version), Arg.Any<CancellationToken>());
        await _dotnet.DidNotReceive().ToolInstall(Arg.Any<ToolInstallArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PinsAtTheRepositoryRootRatherThanTheProject()
    {
        // One manifest at the root serves every project in a repository of several: the SDK finds
        // it by walking up, the same way Ritten finds a project file.
        await Step().Run(TestContext.Current.CancellationToken);

        await _dotnet.Received().ToolInstall(Arg.Is<ToolInstallArgs>(a => a.Scope.Directory == _repository), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FallsBackToTheProjectWhenThereIsNoRepository()
    {
        _git.RepositoryRoot(Arg.Any<CancellationToken>()).Returns((IDirectory?)null);
        var missing = Missing();
        _project.GetFile(Arg.Any<string>()).Returns(missing);

        await Step().Run(TestContext.Current.CancellationToken);

        await _dotnet.Received().ToolInstall(Arg.Is<ToolInstallArgs>(a => a.Scope.Directory == _project), Arg.Any<CancellationToken>());
    }

    /// <summary>What the manifest governing the repository already pins.</summary>
    private void Pinned(NuGetVersion version) =>
        _dotnet.InstalledToolVersion("ritten", Arg.Is<ToolScope>(s => s.Directory == _repository), Arg.Any<CancellationToken>()).Returns(version);

    private EnsureToolManifest Step() =>
        new(Substitute.For<IWorkflowLog>(), _dotnet, _git, _fileSystem, new ToolPin("ritten", "ritten", Version));

    private void SetManifest(bool exists, string at = ".config/dotnet-tools.json")
    {
        var missing = Missing();
        _repository.GetFile(Arg.Any<string>()).Returns(missing);
        if (!exists)
        {
            return;
        }

        var file = Substitute.For<IFile>();
        file.Exists.Returns(true);
        _repository.GetFile(at).Returns(file);
    }

    private static IFile Missing()
    {
        var file = Substitute.For<IFile>();
        file.Exists.Returns(false);
        return file;
    }
}
