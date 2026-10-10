using NuGet.Versioning;
using Ritten.DotNet;
using Ritten.Engine.Workflows;
using Ritten.Releases;
using Ritten.Workflows.DotNetTool;

namespace Ritten.Tests.Engine.Workflows;

public class JobOptionTests
{
    [Fact]
    public void Set_SetsAValueOnArgumentsAHostBuilt()
    {
        var version = ArgumentsModel.For(typeof(PrepareDotNetToolArguments)).Options.ShouldHaveSingleItem();
        var arguments = new PrepareDotNetToolArguments { Build = new DotNetBuildSettings { Project = "src/Thing/Thing.csproj" } };

        version.Set(arguments, version.Read("1.2.0").Value).ShouldBeEmpty();

        arguments.Version.ShouldBe(new RequestedVersion(NuGetVersion.Parse("1.2.0")));
        arguments.Build.Project.ShouldBe("src/Thing/Thing.csproj");
    }

    [Fact]
    public void Set_ReadsAGivenFlagIntoItsPropertyType()
    {
        var force = ArgumentsModel.For(typeof(InstallDotNetToolArguments)).Options.ShouldHaveSingleItem();
        var arguments = new InstallDotNetToolArguments();

        force.Set(arguments, true).ShouldBeEmpty();

        arguments.Reinstall.ShouldBe(new ForceReinstall(true));
    }

    [Fact]
    public void Set_LeavesThePropertyWhenNothingWasGiven()
    {
        var version = ArgumentsModel.For(typeof(PrepareDotNetToolArguments)).Options.ShouldHaveSingleItem();
        var force = ArgumentsModel.For(typeof(InstallDotNetToolArguments)).Options.ShouldHaveSingleItem();
        var prepare = new PrepareDotNetToolArguments();
        var install = new InstallDotNetToolArguments();

        version.Set(prepare, null).ShouldBeEmpty();
        force.Set(install, false).ShouldBeEmpty();

        prepare.Version.ShouldBe(RequestedVersion.None);
        install.Reinstall.ShouldBe(new InstallDotNetToolArguments().Reinstall);
    }

    [Fact]
    public void Read_ReadsAnEnumByItsName() =>
        LinesOption.Read("minor").Value.ShouldBe(ReleaseLine.Minor);

    [Fact]
    public void Read_NamesTheChoicesForAValueThatIsNoneOfThem() =>
        LinesOption.Read("patch").Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldBe("'--lines' takes major or minor, not 'patch'.");

    private static JobOption LinesOption => ArgumentsModel.For(typeof(LinesArguments)).Options.ShouldHaveSingleItem();

    private sealed record LinesArguments
    {
        [CommandLineOption("lines", "How releases are grouped.")]
        public ReleaseLine Lines { get; init; } = ReleaseLine.Major;
    }
}
