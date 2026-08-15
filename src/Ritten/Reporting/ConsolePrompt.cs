using Ritten.Contracts;
using Spectre.Console;

namespace Ritten.Reporting;

/// <summary>
/// Asks for approval at the terminal.
/// </summary>
internal sealed class ConsolePrompt(IAnsiConsole console) : IPipelinePrompt
{
    /// <inheritdoc />
    public bool IsInteractive => console.Profile.Capabilities.Interactive;

    /// <inheritdoc />
    public async Task<bool> Confirm(string consequence, CancellationToken cancellationToken = default)
    {
        console.WriteLine();
        console.MarkupLine($"  [yellow]{Markup.Escape(consequence)}[/]");
        console.MarkupLine("  [grey]Only 'yes' will be accepted.[/]");

        var answer = await console.PromptAsync(
            new TextPrompt<string>("  Enter a value:").AllowEmpty(),
            cancellationToken);

        console.WriteLine();
        return string.Equals(answer.Trim(), "yes", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<string?> Secret(string what, CancellationToken cancellationToken = default)
    {
        console.WriteLine();
        var answer = await console.PromptAsync(
            new TextPrompt<string>($"  {Markup.Escape(what)}").Secret().AllowEmpty(),
            cancellationToken);

        console.WriteLine();
        return string.IsNullOrWhiteSpace(answer) ? null : answer.Trim();
    }
}
