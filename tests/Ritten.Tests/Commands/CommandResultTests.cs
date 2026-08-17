using Ritten.Commands;

namespace Ritten.Tests.Commands;

public class CommandResultTests
{
    [Fact]
    public void ErrorTail_PrefersStandardError()
    {
        var result = new CommandResult(1, "stdout line", "stderr line");

        result.ErrorTail().ShouldBe(["stderr line"]);
    }

    [Fact]
    public void ErrorTail_FallsBackToStandardOutput()
    {
        // dotnet and MSBuild report their errors on stdout, leaving stderr empty.
        var result = new CommandResult(1, "stdout line", "");

        result.ErrorTail().ShouldBe(["stdout line"]);
    }

    [Fact]
    public void ErrorTail_IsEmptyWhenTheCommandWroteNothing()
    {
        new CommandResult(1, "", "").ErrorTail().ShouldBeEmpty();
    }

    [Fact]
    public void ErrorTail_KeepsOnlyTheLastLines()
    {
        var output = string.Join('\n', Enumerable.Range(1, 15).Select(i => $"line {i}"));

        var tail = new CommandResult(1, output, "").ErrorTail();

        tail.Count.ShouldBe(10);
        tail[0].ShouldBe("line 6");
        tail[^1].ShouldBe("line 15");
    }
}
