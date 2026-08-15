using Ritten.Reporting;
using Spectre.Console.Testing;

namespace Ritten.Tests.Reporting;

public class ConsolePromptTests
{
    [Fact]
    public async Task Secret_ReturnsTheTrimmedInputWithoutEchoingIt()
    {
        var console = new TestConsole().Interactive();
        console.Input.PushTextWithEnter("  the-key  ");

        var answer = await new ConsolePrompt(console).Secret("Enter the key:", TestContext.Current.CancellationToken);

        answer.ShouldBe("the-key");
        console.Output.ShouldNotContain("the-key");
    }

    [Fact]
    public async Task Secret_IsNullWhenNothingIsEntered()
    {
        var console = new TestConsole().Interactive();
        console.Input.PushTextWithEnter("");

        var answer = await new ConsolePrompt(console).Secret("Enter the key:", TestContext.Current.CancellationToken);

        answer.ShouldBeNull();
    }
}
