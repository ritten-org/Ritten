using Ritten.Commands;

namespace Ritten.Tests.Commands;

public class CommandTests
{
    [Fact]
    public void Create_SetsThePathAndSafeDefaults()
    {
        var command = Command.Create("git");

        command.Path.ShouldBe("git");
        command.Arguments.ShouldBeEmpty();
        command.WorkingDirectory.ShouldBeNull();
        command.ArgumentsRedacted.ShouldBeFalse();
        command.OutputRedacted.ShouldBeFalse();
        command.ThrowsOnError.ShouldBeFalse();
    }

    [Fact]
    public void WithArguments_ReplacesTheArguments()
    {
        var command = Command.Create("git").WithArguments("tag", "v1").WithArguments("push");

        command.Arguments.ShouldBe(["push"]);
    }

    [Fact]
    public void AndArguments_AppendsToTheArguments()
    {
        var command = Command.Create("git").WithArguments("tag").AndArguments("v1", "abc123");

        command.Arguments.ShouldBe(["tag", "v1", "abc123"]);
    }

    [Fact]
    public void RedactArguments_OnlyRedactsArguments()
    {
        var command = Command.Create("dotnet").RedactArguments();

        command.ArgumentsRedacted.ShouldBeTrue();
        command.OutputRedacted.ShouldBeFalse();
    }

    [Fact]
    public void Sensitive_RedactsArgumentsAndOutput()
    {
        var command = Command.Create("dotnet").Sensitive();

        command.ArgumentsRedacted.ShouldBeTrue();
        command.OutputRedacted.ShouldBeTrue();
    }

    [Fact]
    public void Builders_DoNotMutateTheOriginalCommand()
    {
        var original = Command.Create("git").WithArguments("status");

        original.ThrowOnError().InDirectory("sub").Sensitive();

        original.ThrowsOnError.ShouldBeFalse();
        original.WorkingDirectory.ShouldBeNull();
        original.ArgumentsRedacted.ShouldBeFalse();
    }
}
