using System.CommandLine;
using NuGet.Versioning;
using Ritten.CommandLine;
using Ritten.Engine;
using Ritten.Engine.Workflows;

namespace Ritten.Tests.CommandLine;

/// <summary>
/// The mapping is type-preserving in both directions: what the declaration reads as is what the
/// option parses, and what comes back is the same type without passing through text again.
/// </summary>
public class JobArgumentConverterTests
{
    /// <summary>A type System.CommandLine reads for itself, so the declaration brings no reader.</summary>
    private static readonly JobArgument<string> Message = JobArgument.Value<string>("message", "What to say.", alias: "-m");

    /// <summary>A flag is just a bool, which System.CommandLine already reads as its own presence.</summary>
    private static readonly JobArgument<bool> Force = JobArgument.Value<bool>("force", "Do it anyway.");

    /// <summary>A type no front end can be expected to parse, so the domain reads it.</summary>
    private static readonly JobArgument<NuGetVersion> Version = JobArgument.Value(
        "version",
        "The version to prepare.",
        text => NuGetVersion.TryParse(text, out var version)
            ? new Result<NuGetVersion>(version)
            : Result.Error($"'{text}' is not a version."));

    [Fact]
    public void MapsAnArgumentOntoATypedOption()
    {
        var option = Map(Message).Option;

        option.ShouldBeOfType<Option<string>>();
        option.Name.ShouldBe("--message");
        option.Description.ShouldBe("What to say.");
        option.Required.ShouldBeFalse();
        option.Aliases.ShouldContain("-m");
    }

    [Fact]
    public void ReadsAValueBackAsItsDeclaredType()
    {
        Parse(Message, "--message", "hello").Get(Message).ShouldBe("hello");
        Parse(Version, "--version", "1.2.3").Get(Version).ShouldBe(NuGetVersion.Parse("1.2.3"));
    }

    [Fact]
    public void LeavesAnOmittedValueUnset()
    {
        Parse(Message).Get(Message).ShouldBeNull();
    }

    [Fact]
    public void ReadsAFlagAsItsOwnPresence()
    {
        // Nothing here parses anything: a bool option is presence, which the command line already does.
        Map(Force).Option.ShouldBeOfType<Option<bool>>();
        Parse(Force, "--force").Get(Force).ShouldBeTrue();
        Parse(Force).Get(Force).ShouldBeFalse();
    }

    [Fact]
    public void ReportsWhatTheDomainSaysAboutAValueOnlyItCanRead()
    {
        // The words are the declaration's; the command line only decides where to show them.
        var command = new Command("prepare") { Map(Version).Option };

        var parsed = command.Parse(["prepare", "--version", "next"]);

        parsed.Errors.ShouldHaveSingleItem().Message.ShouldBe("'next' is not a version.");
    }

    private static JobArgumentOption Map(JobArgument argument) => argument.Convert(JobArgumentConverter.Instance);

    private static JobArguments Parse(JobArgument argument, params string[] args)
    {
        var mapped = Map(argument);
        var command = new Command("prepare") { mapped.Option };
        var parsed = command.Parse(["prepare", .. args]);
        parsed.Errors.ShouldBeEmpty();

        var builder = new JobArgumentsBuilder(parsed);
        builder.Add(mapped);
        return builder.Build();
    }
}
