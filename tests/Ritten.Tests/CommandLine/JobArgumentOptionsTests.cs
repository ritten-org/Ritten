using System.CommandLine;
using NuGet.Versioning;
using Ritten.CommandLine;
using Ritten.Engine;
using Ritten.Engine.Workflows;

namespace Ritten.Tests.CommandLine;

/// <summary>
/// The mapping is type-preserving in both directions: what the domain declared is what the option
/// parses, and what comes back is the same type without passing through text again.
/// </summary>
public class JobArgumentOptionsTests
{
    private static readonly JobArgument<NuGetVersion> Version = JobArgument.Value(
        "version",
        "The version to prepare.",
        text => NuGetVersion.TryParse(text, out var version)
            ? new Result<NuGetVersion>(version)
            : Result.Error($"'{text}' is not a version."),
        alias: "-r");

    private static readonly FlagArgument Force = JobArgument.Flag("force", "Do it anyway.");

    [Fact]
    public void MapsAValueOntoATypedOption()
    {
        var mapped = Map(Version);

        mapped.Option.ShouldBeOfType<Option<NuGetVersion>>();
        mapped.Option.Name.ShouldBe("--version");
        mapped.Option.Description.ShouldBe("The version to prepare.");
        mapped.Option.Required.ShouldBeFalse();
    }

    [Fact]
    public void CarriesTheAliasTheDeclarationGave()
    {
        Map(Version).Option.Aliases.ShouldContain("-r");
        Map(Force).Option.Aliases.ShouldBeEmpty();
    }

    [Fact]
    public void ReadsAValueBackAsItsDeclaredType()
    {
        var arguments = Parse(Version, "--version", "1.2.3");

        arguments.Get(Version).ShouldBe(NuGetVersion.Parse("1.2.3"));
    }

    [Fact]
    public void ReportsWhatTheDomainSaysAboutABadValue()
    {
        // The words are the declaration's; the command line only decides where to show them.
        var mapped = Map(Version);
        var command = new Command("prepare") { mapped.Option };

        var parsed = command.Parse(["prepare", "--version", "next"]);

        parsed.Errors.ShouldHaveSingleItem().Message.ShouldBe("'next' is not a version.");
    }

    [Fact]
    public void LeavesAnOmittedValueUnset()
    {
        Parse(Version).Get(Version).ShouldBeNull();
    }

    [Fact]
    public void MapsAFlagOntoItsPresence()
    {
        Map(Force).Option.ShouldBeOfType<Option<bool>>();
        Parse(Force, "--force").IsSet(Force).ShouldBeTrue();
        Parse(Force).IsSet(Force).ShouldBeFalse();
    }

    private static JobArgumentOption Map(JobArgument argument) => argument.Map(new JobArgumentOptions());

    private static JobArguments Parse(JobArgument argument, params string[] args)
    {
        var mapped = Map(argument);
        var command = new Command("prepare") { mapped.Option };
        var parsed = command.Parse(["prepare", .. args]);
        parsed.Errors.ShouldBeEmpty();

        var builder = new JobArgumentsBuilder();
        mapped.Read(parsed, builder);
        return builder.Build();
    }
}
