using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Core;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Git;
using Ritten.Tests.Support;

namespace Ritten.Tests.DotNet;

/// <summary>
/// The repository URL is resolved here, once — explicit setting, then the project file, then
/// the origin remote — so no consumer ever coalesces sources again.
/// </summary>
public class ReadProjectTests
{
    private readonly IDotNet _dotnet = Substitute.For<IDotNet>();
    private readonly IGit _git = Substitute.For<IGit>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly DotNetOptions _options = TestOptions.DotNet();

    public ReadProjectTests()
    {
        var file = Substitute.For<IFile>();
        file.Exists.Returns(true);
        _fileSystem.ProjectRoot.GetFile(_options.ProjectFile).Returns(file);
        FromCsproj(repository: null);
    }

    [Fact]
    public async Task AnExplicitSettingWinsOverEverySource()
    {
        _options.Repository = "https://github.com/configured/repo";
        FromCsproj(repository: "https://github.com/csproj/repo");

        var project = await Produce();

        project.Repository.ShouldBe("https://github.com/configured/repo");
        await _git.DidNotReceiveWithAnyArgs().GetRemoteUrl(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TheProjectFileWinsOverTheRemote()
    {
        FromCsproj(repository: "https://github.com/csproj/repo");

        var project = await Produce();

        project.Repository.ShouldBe("https://github.com/csproj/repo");
        await _git.DidNotReceiveWithAnyArgs().GetRemoteUrl(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FallsBackToTheOriginRemoteNormalised()
    {
        _git.GetRemoteUrl("origin", Arg.Any<CancellationToken>()).Returns("git@github.com:remote/repo.git");

        var project = await Produce();

        project.Repository.ShouldBe("https://github.com/remote/repo");
    }

    [Fact]
    public async Task LeavesTheRepositoryUnknownWhenNoSourceHasIt()
    {
        var project = await Produce();

        project.Repository.ShouldBeNull();
    }

    [Fact]
    public async Task FailsWhenTheProjectFileDoesNotExist()
    {
        var missing = Substitute.For<IFile>();
        missing.Exists.Returns(false);
        _fileSystem.ProjectRoot.GetFile(_options.ProjectFile).Returns(missing);

        var result = await Step().Run(TestContext.Current.CancellationToken);

        result.Outcome.IsFailure.ShouldBeTrue();
    }

    private void FromCsproj(string? repository) =>
        _dotnet.ReadProject(Arg.Any<IFile>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new Project
            {
                Name = "My.Package",
                Version = NuGetVersion.Parse("1.2.0"),
                Repository = repository
            }));

    private async Task<Project> Produce()
    {
        var result = await Step().Run(TestContext.Current.CancellationToken);
        result.Outcome.IsFailure.ShouldBeFalse();
        return result.Value.ShouldNotBeNull();
    }

    private ReadProject Step() =>
        new(Substitute.For<IPipelineLog>(), Options.Create(_options), _fileSystem, _dotnet, _git);
}
