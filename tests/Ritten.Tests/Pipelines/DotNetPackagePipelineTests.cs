using Ritten.Pipelines.DotNet;

namespace Ritten.Tests.Pipelines;

/// <summary>
/// The three .NET package pipelines share a settings type but not its requirements: only the ones
/// that ship a package need to know which project to pack.
/// </summary>
public class DotNetPackagePipelineTests
{
    [Fact]
    public void Verify_DoesNotRequireAProject()
    {
        var failures = new DotNetPackageVerify().Validate(new DotNetPackageSettings());

        failures.ShouldBeEmpty();
    }

    [Fact]
    public void Build_RequiresAProject()
    {
        var failures = new DotNetPackageBuild().Validate(new DotNetPackageSettings());

        failures.ShouldHaveSingleItem().ShouldContain("'project' in ritten.json");
    }

    [Fact]
    public void Deploy_RequiresAProject()
    {
        var failures = new DotNetPackageDeploy().Validate(new DotNetPackageSettings());

        failures.ShouldHaveSingleItem().ShouldContain("'project' in ritten.json");
    }

    [Fact]
    public void Build_AcceptsSettingsWithAProject()
    {
        var settings = new DotNetPackageSettings { Project = "src/Thing/Thing.csproj" };

        new DotNetPackageBuild().Validate(settings).ShouldBeEmpty();
    }

    [Fact]
    public void Settings_DeclareTheCapabilitiesThePipelinesUse()
    {
        var settings = new DotNetPackageSettings();

        // The interfaces are what let the capability-scoped registrations accept these settings.
        settings.ShouldBeAssignableTo<Ritten.Core.Settings.IDotNetSettings>();
        settings.ShouldBeAssignableTo<Ritten.Core.Settings.IChangelogSettings>();
        settings.ShouldBeAssignableTo<Ritten.Core.Settings.ITagSettings>();
        settings.ShouldBeAssignableTo<Ritten.Core.Settings.INuGetSettings>();
    }
}
