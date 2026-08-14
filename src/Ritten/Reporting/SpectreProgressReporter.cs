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

    /// <inheritdoc />
    public Task OnPipelineStarted(PipelineJob job, CancellationToken cancellationToken)
    {
        console.Write(new Rule($"[bold]{Markup.Escape(job.Pipeline)}[/] [grey]·[/] [bold]{Markup.Escape(job.Name)}[/]").LeftJustified());
        _pipelineTimer.Restart();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnStepStarted(IPipelineStep step, CancellationToken cancellationToken)
    {
        _stepTimer.Restart();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnStepCompleted(IPipelineStep step, StepResult result, CancellationToken cancellationToken)
    {
        var name = Markup.Escape(step.Name);
        var elapsed = FormatDuration(_stepTimer.Elapsed);

        if (result.IsFailure)
        {
            console.MarkupLine($"  [red]✗ {name}[/] [grey]{elapsed}[/]");
            console.MarkupLine($"    [red]{Markup.Escape(result.Message)}[/]");
        }
        else
        {
            console.MarkupLine($"  [green]✓ {name}[/] [grey]{elapsed}[/]");
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
            console.MarkupLine(level switch
            {
                PipelineLogLevel.Status => $"  [grey]{text}[/]",
                PipelineLogLevel.Verbose => $"    [grey italic]{text}[/]",
                PipelineLogLevel.Warning => $"  [yellow]⚠ {text}[/]",
                PipelineLogLevel.Error => $"  [red]✗ {text}[/]",
                _ => $"    [grey]{text}[/]"
            });
        }

        if (exception != null)
        {
            var format = minimumLogLevel switch
            {
                // --verbose
                PipelineLogLevel.Verbose => ExceptionFormats.Default | ExceptionFormats.ShowLinks,

                // Default
                PipelineLogLevel.Detail => ExceptionFormats.ShortenEverything,

                // --quiet
                _ => ExceptionFormats.NoStackTrace,
            };

            var paddedEx = new Padder(exception.GetRenderable(format)).PadLeft(4).PadTop(0).PadBottom(0).PadRight(0);
            console.Write(paddedEx);
        }
    }


    private static string FormatDuration(TimeSpan elapsed) =>
        elapsed.TotalMinutes >= 1 ? $"{elapsed.TotalMinutes:0.0}m" : $"{elapsed.TotalSeconds:0.0}s";
}
