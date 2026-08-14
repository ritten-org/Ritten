using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Core.Runner;

namespace Ritten.Core;

/// <summary>
/// Represents a pipeline application that can be run.
/// </summary>
public class RittenApplication : IDisposable
{
    private readonly ServiceProvider _services;

    internal RittenApplication(ServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _services.Dispose();
    }

    /// <summary>
    /// Creates, configures, and runs the specified pipeline, returning its exit code.
    /// </summary>
    /// <typeparam name="TPipeline">The pipeline type to run.</typeparam>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public static async Task<int> Run<TPipeline>(CancellationToken cancellationToken = default) where TPipeline : Pipeline, new()
    {
        var builder = new RittenApplicationBuilder();
        var pipeline = new TPipeline();
        pipeline.Configure(builder);
        builder.Services.AddSingleton<Pipeline>(pipeline);

        using var app = builder.Build();
        return await app.Run(cancellationToken);
    }

    internal async Task<int> Run(CancellationToken cancellationToken)
    {
        var log = _services.GetRequiredService<IPipelineLog>();

        if (!TryValidate(out var failures))
        {
            log.Error(failures.Count == 1 ? "Configuration error:" : $"Configuration errors ({failures.Count}):");
            foreach (var failure in failures)
            {
                log.Error($"  • {failure}");
            }

            return PipelineExitCodes.ConfigurationError;
        }

        var runner = _services.GetRequiredService<IPipelineRunner>();
        var result = await runner.Run(cancellationToken);
        return result.ExitCode;
    }

    /// <summary>
    /// Validates every options type registered with <c>ValidateOnStart()</c>. A generic host would
    /// run this as part of starting; Ritten has no host, so it drives the validator itself, before
    /// the first step runs rather than whenever a step first reads its options.
    /// </summary>
    private bool TryValidate(out IReadOnlyList<string> failures)
    {
        failures = [];
        if (_services.GetService<IStartupValidator>() is not { } validator)
        {
            return true;
        }

        try
        {
            validator.Validate();
            return true;
        }
        catch (Exception exception)
        {
            failures = [.. Flatten(exception)];
            return false;
        }
    }

    // One failing options type surfaces as an OptionsValidationException;
    // several are wrapped in an AggregateException.
    private static IEnumerable<string> Flatten(Exception exception) => exception switch
    {
        AggregateException aggregate => aggregate.InnerExceptions.SelectMany(Flatten),
        OptionsValidationException validation => validation.Failures,
        _ => [exception.Message]
    };
}
