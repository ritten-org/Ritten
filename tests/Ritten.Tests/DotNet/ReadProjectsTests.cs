using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.DotNet;

public class ReadProjectsTests
{
    private readonly IDotNet _dotnet = Substitute.For<IDotNet>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly DotNetOptions _options = TestOptions.DotNet();

    [Fact]
    public async Task ReadsEveryConfiguredPackage()
    {
        _options.Projects = ["src/Core/Core.csproj", "src/Tool/Tool.csproj"];
        Package("src/Core/Core.csproj", "My.Package.Core", "1.2.0");
        Package("src/Tool/Tool.csproj", "My.Package", "1.2.0");

        var result = await Step().Run(TestContext.Current.CancellationToken);

        var packages = result.Value.ShouldNotBeNull().Packages;
        packages.Select(p => p.Name).ShouldBe(["My.Package.Core", "My.Package"]);
        // The file each was read from travels with it: pack needs to know what to pack.
        packages.Select(p => p.ProjectFile).ShouldBe(["src/Core/Core.csproj", "src/Tool/Tool.csproj"]);
    }

    [Fact]
    public async Task FailsWhenAPackageProjectIsMissing()
    {
        _options.Projects = ["src/Gone/Gone.csproj"];
        var file = Substitute.For<IFile>();
        file.Exists.Returns(false);
        _fileSystem.ProjectRoot.GetFile("src/Gone/Gone.csproj").Returns(file);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
        result.Outcome.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("src/Gone/Gone.csproj");
    }

    private void Package(string path, string name, string version)
    {
        var file = Substitute.For<IFile>();
        file.Exists.Returns(true);
        _fileSystem.ProjectRoot.GetFile(path).Returns(file);
        _dotnet.ReadProject(file, Arg.Any<CancellationToken>())
            .Returns(new Project { Name = name, Version = NuGetVersion.Parse(version) });
    }

    private ReadProjects Step() =>
        new(Substitute.For<IWorkflowLog>(), Options.Create(_options), _fileSystem, _dotnet);
}
