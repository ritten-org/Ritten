using System.Diagnostics;
using Ritten.Contracts;
using Ritten.Engine.Runs;
using Spectre.Console;

namespace Ritten.Reporting;

/// <summary>
/// Renders workflow progress to the terminal using Spectre.Console.
/// </summary>
internal sealed class SpectreWorkflowConsole(IAnsiConsole console, WorkflowLogLevel minimumLogLevel) : IWorkflowConsole
{
    private readonly Stopwatch _stepTimer = new();
    private readonly Stopwatch _workflowTimer = new();

    /// <inheritdoc />
    public Task OnWorkflowStarted(WorkflowJob job, CancellationToken cancellationToken)
    {
        // Said out loud, because the whole point is that nothing durable happened.
        var dryRun = job.DryRun ? " [grey]·[/] [yellow]dry run[/]" : "";
        console.Write(new Rule($"[bold]{Markup.Escape(job.Workflow)}[/] [grey]·[/] [bold]{Markup.Escape(job.Name)}[/]{dryRun}").LeftJustified());
        _workflowTimer.Restart();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnStepStarted(Step step, CancellationToken cancellationToken)
    {
        _stepTimer.Restart();

        // The name opens the step and the outcome closes it, so that anything the step says
        // reads as its body. Chronology would put the name last, which reads backwards.
        // Headings and outcomes are the job's structure, not chatter: they render at every
        // level, and --quiet silences only what the steps say in between.
        WriteHeading(step);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnStepCompleted(Step step, StepResult result, CancellationToken cancellationToken)
    {
        var elapsed = FormatDuration(_stepTimer.Elapsed);

        if (result.IsFailure)
        {
            Write(2, $"[red]✗[/] [grey]{elapsed}[/]");
            foreach (var error in result.Errors)
            {
                Write(4, $"[red]{Markup.Escape(error.Message)}[/]");
                WriteVerbatim(error.Verbatim);
            }
        }
        else
        {
            Write(2, $"[green]✓[/] [grey]{elapsed}[/]");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnWorkflowCompleted(WorkflowResult result, CancellationToken cancellationToken)
    {
        _workflowTimer.Stop();
        var passed = result.Steps.Count(s => !s.Result.IsFailure);
        var failed = result.Steps.Count(s => s.Result.IsFailure);
        var total = result.Steps.Count;
        var elapsed = FormatDuration(_workflowTimer.Elapsed);

        var color = result.IsSuccess ? "green" : "red";
        var summary = failed > 0
            ? $"{total} steps in {elapsed} ({passed} passed, {failed} failed)"
            : $"{total} steps in {elapsed}";

        console.Write(new Rule($"[{color}]{summary}[/]").LeftJustified());
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public bool IsEnabled(WorkflowLogLevel level) => level >= minimumLogLevel;

    /// <inheritdoc />
    public void Log(WorkflowLogLevel level, string? message, Exception? exception = null)
    {
        if (!IsEnabled(level))
        {
            return;
        }

        if (message != null)
        {
            var text = Markup.Escape(message);
            var (indent, markup) = level switch
            {
                WorkflowLogLevel.Status => (4, $"[grey]{text}[/]"),
                WorkflowLogLevel.Skipped => (4, $"[mediumpurple]⊘ {text}[/]"),
                WorkflowLogLevel.Verbose => (4, $"[grey italic]{text}[/]"),
                WorkflowLogLevel.Warning => (2, $"[yellow]⚠ {text}[/]"),
                WorkflowLogLevel.Error => (2, $"[red]✗ {text}[/]"),
                _ => (4, $"[grey]{text}[/]")
            };

            Write(indent, markup);
        }

        if (exception != null && IsEnabled(WorkflowLogLevel.Verbose))
        {
            var renderable = exception.GetRenderable(ExceptionFormats.Default | ExceptionFormats.ShowLinks);
            console.Write(new Padder(renderable).PadLeft(4).PadTop(0).PadBottom(0).PadRight(0));
        }
    }


    /// <summary>
    /// Writes content the reader is meant to copy: straight to the underlying writer.
    /// </summary>
    private void WriteVerbatim(string? text)
    {
        if (text is null)
        {
            return;
        }

        var writer = console.Profile.Out.Writer;
        writer.WriteLine();
        foreach (var line in text.Split('\n'))
        {
            writer.WriteLine(line.TrimEnd());
        }

        writer.WriteLine();
    }

    private void WriteHeading(Step step)
    {
        var (glyph, color) = Style(step.Kind);
        Write(2, $"[{color}]{glyph}[/] [bold]{Markup.Escape(step.Name)}[/]");
    }

    /// <summary>
    /// Each kind keeps a stable glyph and colour, so a job's shape reads at a glance.
    /// </summary>
    private static (string Glyph, string Color) Style(StepKind kind) => kind switch
    {
        StepKind.Check => ("○", "deepskyblue1"),
        StepKind.Gate => ("◆", "yellow"),
        StepKind.Publish => ("▲", "fuchsia"),
        _ => ("·", "grey")
    };

    /// <summary>
    /// Writes indented markup. A <see cref="Padder"/> rather than leading spaces, so that a line
    /// too long for the terminal keeps its indent when it wraps instead of falling back to the
    /// left margin — which matters now that indentation is what nests a step's output under it.
    /// </summary>
    private void Write(int indent, string markup) =>
        console.Write(new Padder(new Markup(markup)).PadLeft(indent).PadTop(0).PadBottom(0).PadRight(0));

    private static string FormatDuration(TimeSpan elapsed) =>
        elapsed.TotalMinutes >= 1 ? $"{elapsed.TotalMinutes:0.0}m" : $"{elapsed.TotalSeconds:0.0}s";
}
