using System.Diagnostics;
using Ritten.Contracts;
using Ritten.Core;
using Spectre.Console;

namespace Ritten.Reporting;

/// <summary>
/// Renders pipeline progress to the terminal using Spectre.Console.
/// </summary>
internal sealed class SpectreProgressReporter(IAnsiConsole console, PipelineLogLevel minimumLogLevel) : IProgressReporter, IPipelineLog
{
    private readonly Stopwatch _stepTimer = new();
    private readonly Stopwatch _pipelineTimer = new();
    private bool _headingWritten;

    /// <inheritdoc />
    public Task OnPipelineStarted(PipelineJob job, CancellationToken cancellationToken)
    {
        // Said out loud, because the whole point is that nothing durable happened.
        var dryRun = job.DryRun ? " [grey]·[/] [yellow]dry run[/]" : "";
        console.Write(new Rule($"[bold]{Markup.Escape(job.Pipeline)}[/] [grey]·[/] [bold]{Markup.Escape(job.Name)}[/]{dryRun}").LeftJustified());
        _pipelineTimer.Restart();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnStepStarted(IPipelineStep step, CancellationToken cancellationToken)
    {
        _stepTimer.Restart();
        _headingWritten = false;

        // The name opens the step and the outcome closes it, so that anything the step says
        // reads as its body. Chronology would put the name last, which reads backwards.
        if (IsEnabled(PipelineLogLevel.Status))
        {
            WriteHeading(step);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnStepCompleted(IPipelineStep step, StepResult result, CancellationToken cancellationToken)
    {
        var elapsed = FormatDuration(_stepTimer.Elapsed);

        if (result.IsFailure)
        {
            // A failure is shown however quiet the run is, so it may still need its heading.
            if (!_headingWritten)
            {
                WriteHeading(step);
            }

            Write(2, $"[red]✗[/] [grey]{elapsed}[/]");
            foreach (var error in result.Errors)
            {
                Write(4, $"[red]{Markup.Escape(error.Message)}[/]");
                WriteVerbatim(error.Verbatim);
            }
        }
        else if (IsEnabled(PipelineLogLevel.Status))
        {
            Write(2, $"[green]✓[/] [grey]{elapsed}[/]");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnPipelineCompleted(PipelineResult result, CancellationToken cancellationToken)
    {
        _pipelineTimer.Stop();
        var passed = result.Steps.Count(s => !s.IsFailure);
        var failed = result.Steps.Count(s => s.IsFailure);
        var total = result.Steps.Count;
        var elapsed = FormatDuration(_pipelineTimer.Elapsed);

        var color = result.IsSuccess ? "green" : "red";
        var summary = failed > 0
            ? $"{total} steps in {elapsed} ({passed} passed, {failed} failed)"
            : $"{total} steps in {elapsed}";

        console.Write(new Rule($"[{color}]{summary}[/]").LeftJustified());
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public bool IsEnabled(PipelineLogLevel level) => level >= minimumLogLevel;

    /// <inheritdoc />
    public void Log(PipelineLogLevel level, string? message, Exception? exception = null)
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
                PipelineLogLevel.Status => (4, $"[grey]{text}[/]"),
                PipelineLogLevel.Skipped => (4, $"[mediumpurple]⊘ {text}[/]"),
                PipelineLogLevel.Verbose => (4, $"[grey italic]{text}[/]"),
                PipelineLogLevel.Warning => (2, $"[yellow]⚠ {text}[/]"),
                PipelineLogLevel.Error => (2, $"[red]✗ {text}[/]"),
                _ => (4, $"[grey]{text}[/]")
            };

            Write(indent, markup);
        }

        if (exception != null && IsEnabled(PipelineLogLevel.Verbose))
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

    private void WriteHeading(IPipelineStep step)
    {
        Write(2, $"[bold]{Markup.Escape(step.Name)}[/]");
        _headingWritten = true;
    }

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
