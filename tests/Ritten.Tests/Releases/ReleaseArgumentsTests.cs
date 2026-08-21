using NuGet.Versioning;
using Ritten.Engine;
using Ritten.Releases;

namespace Ritten.Tests.Releases;

/// <summary>
/// A version is a type no command line can be expected to parse, so the declaration reads it —
/// and a bad one is refused where it was given rather than by whichever step eventually needed it.
/// </summary>
public class ReleaseArgumentsTests
{
    [Fact]
    public void ReadsAVersion()
    {
        var read = Read("1.2.0-beta.1");

        read.IsSuccess.ShouldBeTrue();
        read.Value.ShouldBe(NuGetVersion.Parse("1.2.0-beta.1"));
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
        // Prepare derives a version when nobody names one, so requiring it would defeat the point.
        ReleaseArguments.Version.Required.ShouldBeFalse();
    }

    private static Result<NuGetVersion> Read(string text) =>
        ReleaseArguments.Version.Parse.ShouldNotBeNull()(text);
}
