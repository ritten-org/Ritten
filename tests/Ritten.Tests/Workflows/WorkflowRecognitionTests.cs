using Ritten.Engine.FileSystem;
using Ritten.Engine.Workflows;
using Ritten.Workflows.DotNet;
using Ritten.Workflows.DotNetPackage;
using Ritten.Workflows.DotNetTool;

namespace Ritten.Tests.Workflows;

/// <summary>
/// A repository that hasn't declared a workflow is read the way a person would read it, by each
/// workflow in turn: the registry asks in registration order and the first to recognise it wins,
/// because every tool repository is also a package repository.
/// </summary>
public class WorkflowRecognitionTests : IDisposable
{
    private const string Tool =
        """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <PackAsTool>true</PackAsTool>
          </PropertyGroup>
        </Project>
        """;

    private const string Package =
        """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <PackageId>My.Package</PackageId>
          </PropertyGroup>
        </Project>
        """;

    private const string Nothing = "<Project Sdk=\"Microsoft.NET.Sdk\" />";

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ritten-recognition-{Guid.NewGuid():N}");

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Fact]
    public async Task ARepositoryThatPacksAToolIsAToolRepository()
    {
        Project("src/My.Tool/My.Tool.csproj", Tool);

        var recognised = await Registry().IsCompatible(new PhysicalDirectory(_root), TestContext.Current.CancellationToken);

        recognised.ShouldNotBeNull().Workflow.Name.ShouldBe("dotnet-tool");
        recognised.Reason.ShouldBe("src/My.Tool/My.Tool.csproj packs as a tool");
    }

    [Fact]
    public async Task APropertySharedByEveryProjectCounts()
    {
        // A property true of every project is usually declared once, in the shared build props.
        Project("src/My.Tool/My.Tool.csproj", Nothing);
        Project("Directory.Build.props", "<Project><PropertyGroup><PackAsTool>true</PackAsTool></PropertyGroup></Project>");

        var recognised = await Registry().IsCompatible(new PhysicalDirectory(_root), TestContext.Current.CancellationToken);

        recognised.ShouldNotBeNull().Workflow.Name.ShouldBe("dotnet-tool");
    }

    [Fact]
    public async Task ARepositoryThatPacksAPackageIsAPackageRepository()
    {
        Project("src/My.Package/My.Package.csproj", Package);

        var recognised = await Registry().IsCompatible(new PhysicalDirectory(_root), TestContext.Current.CancellationToken);

        recognised.ShouldNotBeNull().Workflow.Name.ShouldBe("dotnet-package");
    }

    [Fact]
    public async Task ARepositoryThatShipsNothingStillBuilds()
    {
        Project("src/App/App.csproj", Nothing);

        var recognised = await Registry().IsCompatible(new PhysicalDirectory(_root), TestContext.Current.CancellationToken);

        recognised.ShouldNotBeNull().Workflow.Name.ShouldBe("dotnet");
    }

    [Fact]
    public async Task ADirectoryWithNoProjectsIsRecognisedByNobody()
    {
        Directory.CreateDirectory(_root);

        var recognised = await Registry().IsCompatible(new PhysicalDirectory(_root), TestContext.Current.CancellationToken);

        recognised.ShouldBeNull();
    }

    /// <summary>The registry as the tool registers it: most specific first.</summary>
    private static WorkflowRegistry Registry() => new WorkflowRegistry()
        .Add<DotNetToolWorkflow>()
        .Add<DotNetPackageWorkflow>()
        .Add<DotNetWorkflow>();

    private void Project(string path, string content)
    {
        var file = Path.Combine(_root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content);
    }
}
