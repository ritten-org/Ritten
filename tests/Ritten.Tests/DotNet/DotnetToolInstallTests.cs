using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Reporting;

namespace Ritten.Tests.DotNet;

public class DotnetToolInstallTests
{
    private readonly IDotNet _dotnet = Substitute.For<IDotNet>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly IWorkflowLog _log = Substitute.For<IWorkflowLog>();

    [Fact]
    public async Task InstallsAFreshTool()
    {
        _dotnet.InstalledToolVersion("My.Tool", Arg.Any<CancellationToken>()).Returns((NuGetVersion?)null);

        var result = await Step().Run(Tool("1.2.0"), Packed("My.Tool.1.2.0.nupkg"), TestContext.Current.CancellationToken);

        result.ShouldBe(StepResult.Successful);
        await _dotnet.DidNotReceive().ToolUninstall(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _dotnet.Received().ToolInstall(
            Arg.Is<ToolInstallArgs>(a => a.PackageId == "My.Tool" && a.Version == NuGetVersion.Parse("1.2.0") && a.Source == _fileSystem.Artifacts),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StopsWhenThisVersionIsAlreadyInstalled()
    {
        _dotnet.InstalledToolVersion("My.Tool", Arg.Any<CancellationToken>()).Returns(NuGetVersion.Parse("1.2.0"));

        var result = await Step().Run(Tool("1.2.0"), Packed("My.Tool.1.2.0.nupkg"), TestContext.Current.CancellationToken);

        // Nothing left to do — but the hint tells how to insist.
        result.ShouldBe(StepResult.NothingToDo);
        await _dotnet.DidNotReceive().ToolInstall(Arg.Any<ToolInstallArgs>(), Arg.Any<CancellationToken>());
        _log.Received().Log(WorkflowLogLevel.Skipped, Arg.Is<string?>(m => m != null && m.Contains("--force")));
    }

    [Fact]
    public async Task ReinstallsTheSameVersionWithForce()
    {
        _dotnet.InstalledToolVersion("My.Tool", Arg.Any<CancellationToken>()).Returns(NuGetVersion.Parse("1.2.0"));

        var result = await Step(force: true).Run(Tool("1.2.0"), Packed("My.Tool.1.2.0.nupkg"), TestContext.Current.CancellationToken);

        result.ShouldBe(StepResult.Successful);
        await _dotnet.Received().ToolUninstall("My.Tool", Arg.Any<CancellationToken>());
        await _dotnet.Received().ToolInstall(Arg.Any<ToolInstallArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplacesADifferentVersionWithoutForce()
    {
        // Moving to the working tree's version is the job's ordinary work; force only guards
        // repeating an install that already matches.
        _dotnet.InstalledToolVersion("My.Tool", Arg.Any<CancellationToken>()).Returns(NuGetVersion.Parse("1.1.0"));

        var result = await Step().Run(Tool("1.2.0"), Packed("My.Tool.1.2.0.nupkg"), TestContext.Current.CancellationToken);

        result.ShouldBe(StepResult.Successful);
        await _dotnet.Received().ToolUninstall("My.Tool", Arg.Any<CancellationToken>());
        await _dotnet.Received().ToolInstall(
            Arg.Is<ToolInstallArgs>(a => a.Version == NuGetVersion.Parse("1.2.0")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FailsWhenNoProjectPacksAsATool()
    {
        var packages = new PackageSet
        {
            Packages = [new Project { Name = "My.Library", Version = NuGetVersion.Parse("1.2.0") }]
        };

        var result = await Step().Run(packages, Packed("My.Library.1.2.0.nupkg"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("PackAsTool");
        await _dotnet.DidNotReceive().ToolInstall(Arg.Any<ToolInstallArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FailsWhenTheToolWasNotPacked()
    {
        var result = await Step().Run(Tool("1.2.0"), Packed("Other.Package.1.2.0.nupkg"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("My.Tool.1.2.0.nupkg");
        await _dotnet.DidNotReceive().ToolInstall(Arg.Any<ToolInstallArgs>(), Arg.Any<CancellationToken>());
    }

    private static PackageSet Tool(string version) => new()
    {
        Packages =
        [
            new Project { Name = "My.Tool", Version = NuGetVersion.Parse(version), IsTool = true, ToolCommand = "mytool" }
        ]
    };

    private static PackResult Packed(params string[] names) => new()
    {
        Packages = [.. names.Select(name =>
        {
            var file = Substitute.For<IFile>();
            file.Name.Returns(name);
            return file;
        })]
    };

    private DotnetToolInstall Step(bool force = false) =>
        new(new WorkflowJob("dotnet tool", "install"), new ForceReinstall(force), _log, _fileSystem, _dotnet);
}
