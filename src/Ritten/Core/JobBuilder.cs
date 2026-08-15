using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;

namespace Ritten.Core;

internal sealed class JobBuilder(IServiceCollection services, IPipelineLog log, Func<string, string?> environment, bool dryRun) : IJobBuilder
{
    private readonly List<Type> _stepTypes = [];
    private readonly List<Error> _errors = [];

    /// <summary>
    /// Turns <c>settings.Build.Project</c> into <c>build.project</c>.
    /// </summary>
    private static string SettingKey(string expression)
    {
        var segments = expression.Split('.');
        return string.Join('.', segments
            .Skip(segments.Length > 1 ? 1 : 0)
            .Select(JsonNamingPolicy.CamelCase.ConvertName));
    }

    /// <inheritdoc/>
    public IJobBuilder Requires(string? value, string expression = "")
    {
        if (string.IsNullOrEmpty(value))
        {
            _errors.Add($"'{SettingKey(expression)}' not set in {RittenProject.FileName}.");
        }

        return this;
    }

    /// <inheritdoc/>
    public IJobBuilder RequiresEnvironment(string variable)
    {
        if (!string.IsNullOrEmpty(environment(variable)))
        {
            return this;
        }

        if (dryRun)
        {
            // A rehearsal can finish without it, but finding out that the real run couldn't
            // is most of what a rehearsal is for. Warned, not failed.
            log.Warning($"{variable} is not set; a real run would stop before starting.");
        }
        else
        {
            _errors.Add($"{variable} is not set.");
        }

        return this;
    }

    /// <inheritdoc/>
    public IJobBuilder UseStep<TStep>() where TStep : class, IPipelineStep
    {
        _stepTypes.Add(typeof(TStep));
        return this;
    }

    /// <summary>
    /// Builds the job and returns the steps to run.
    /// </summary>
    public Result<IReadOnlyList<StepDescriptor>> Build()
    {
        List<StepDescriptor> steps = [];
        List<Error> errors = [.. _errors];
        foreach (var stepType in _stepTypes)
        {
            var step = StepDescriptor.Describe(stepType);
            if (step.IsError)
            {
                errors.AddRange(step.Errors);
                continue;
            }

            steps.Add(step.Value);
            services.AddTransient(stepType);
        }

        if (errors.Count > 0)
        {
            return errors;
        }
        return steps;
    }
}
