using System.Diagnostics;
using Ritten.Contracts;
using Ritten.Core;
using Spectre.Console;

namespace Ritten.Reporting;

/// <summary>
/// Renders pipeline progress to the terminal using Spectre.Console.
/// </summary>
internal sealed class SpectreProgressReporter(IAnsiConsole console) : IProgressReporter, IPipelineLog
{
    private readonly Stopwatch _stepTimer = new();
    private readonly Stopwatch _pipelineTimer = new();

    /// <inheritdoc />
    public Task OnPipelineStarted(Pipeline pipeline, CancellationToken cancellationToken)
    {
        console.Write(new Rule($"[bold]{Markup.Escape(pipeline.Name)}[/]").LeftJustified());
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
    public void Status(string message) =>
        console.MarkupLine($"  [grey]{Markup.Escape(message)}[/]");

    /// <inheritdoc />
    public void Detail(string message) =>
        console.MarkupLine($"    [grey]{Markup.Escape(message)}[/]");

    private static string FormatDuration(TimeSpan elapsed) =>
        elapsed.TotalMinutes >= 1 ? $"{elapsed.TotalMinutes:0.0}m" : $"{elapsed.TotalSeconds:0.0}s";
}
