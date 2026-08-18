using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Contracts.FileSystem;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Reporting;
using Ritten.Tests.Support;

namespace Ritten.Tests.DotNet;

public class DotnetPackTests
{
    private readonly IDotNet _dotnet = Substitute.For<IDotNet>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly DotNetOptions _options = TestOptions.DotNet();

    [Fact]
    public async Task PacksEveryShippedPackage()
    {
        var packages = new PackageSet
        {
            Packages =
            [
                new Project { Name = "My.Package.Core", Version = NuGetVersion.Parse("1.2.0"), ProjectFile = "src/Core/Core.csproj" },
                new Project { Name = "My.Package", Version = NuGetVersion.Parse("1.2.0"), ProjectFile = "src/Tool/Tool.csproj" }
            ]
        };
        var packed = new PackResult { Packages = [Substitute.For<IFile>()] };
        _dotnet.Pack(Arg.Any<PackArgs>(), Arg.Any<CancellationToken>()).Returns(packed);

        var result = await Step().Run(packages, TestContext.Current.CancellationToken);

        await _dotnet.Received().Pack(Arg.Is<PackArgs>(a => a.Project == "src/Core/Core.csproj"), Arg.Any<CancellationToken>());
        await _dotnet.Received().Pack(Arg.Is<PackArgs>(a => a.Project == "src/Tool/Tool.csproj"), Arg.Any<CancellationToken>());
        result.Value.ShouldBe(packed);
    }

    private DotnetPack Step() =>
        new(Substitute.For<IWorkflowLog>(), Options.Create(_options), _fileSystem, _dotnet);
}
