using NuGet.Versioning;
using Ritten.Releases;

namespace Ritten.Tests.Releases;

/// <summary>
/// The declaration owns reading its own text, so a bad value is refused where it was given
/// rather than by whichever step eventually needed it.
/// </summary>
public class ReleaseArgumentsTests
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
        // Prepare derives a version when nobody names one, so requiring it would defeat the point.
        ReleaseArguments.Version.Required.ShouldBeFalse();
        ReleaseArguments.Version.TakesValue.ShouldBeTrue();
    }

    private static Ritten.Engine.Result<RequestedVersion> Read(string text) => ReleaseArguments.Version.Read(text);
}
