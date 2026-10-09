using NuGet.Versioning;
using Ritten.Engine;
using Ritten.Engine.Workflows;
using Ritten.Releases;
using Ritten.Workflows.DotNetTool;

namespace Ritten.Tests.Releases;

/// <summary>
/// A version is a type no command line can be expected to parse, so the version's own type reads it —
/// and a bad one is refused where it was given rather than by whichever step eventually needed it.
/// </summary>
public class RequestedVersionTests
{
    [Fact]
    public void ReadsAVersion()
    {
        var read = Read("1.2.0-beta.1");

        read.IsSuccess.ShouldBeTrue();
        read.Value.ShouldBe(new RequestedVersion(NuGetVersion.Parse("1.2.0-beta.1")));
    }

    [Fact]
    public void RefusesTextThatIsNotAVersion()
    {
        var read = Read("next");

        read.IsError.ShouldBeTrue();
        read.Errors.ShouldHaveSingleItem().Message.ShouldBe("'next' is not a version. Give one like 1.2.0.");
    }

    [Fact]
    public void IsOptional()
    {
        // Prepare derives a version when nobody names one, so it starts as none.
        new PrepareDotNetToolArguments().Version.ShouldBe(RequestedVersion.None);
    }

    private static Result<object> Read(string text) =>
        ArgumentsModel.For(typeof(PrepareDotNetToolArguments)).Options.Single(o => o.Name == RequestedVersion.OptionName).Read(text);
}
