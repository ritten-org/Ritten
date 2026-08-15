using System.Reflection;
using System.Runtime.ExceptionServices;
using Ritten.Contracts;

namespace Ritten.Core;

/// <summary>
/// A step's of a pipeline step and the details needed to run it.
/// </summary>
internal sealed class StepDescriptor
{
    private enum ParameterSource
    {
        Token,
        Optional,
        Required
    }

    private readonly MethodInfo _run;
    private readonly PropertyInfo _taskResult;
    private readonly IReadOnlyList<(Type Type, ParameterSource Source)> _parameters;

    private StepDescriptor(Type stepType, StepAttribute metadata, MethodInfo run, Type? produces, IReadOnlyList<(Type Type, ParameterSource Source)> parameters)
    {
        StepType = stepType;
        Produces = produces;
        _run = run;
        _parameters = parameters;
        _taskResult = run.ReturnType.GetProperty(nameof(Task<>.Result))!;

        Step = new JobStep(
            metadata.Name,
            metadata.Kind,
            produces,
            [.. parameters.Where(p => p.Source == ParameterSource.Required).Select(p => p.Type)]);
    }

    /// <summary>
    /// The step type declaring the <c>Run</c> method.
    /// </summary>
    public Type StepType { get; }

    /// <summary>
    /// The type this step produces into pipeline state, or <c>null</c> for a non-producing step.
    /// </summary>
    public Type? Produces { get; }

    /// <summary>
    /// The step as rules and reporters see it, read entirely from the type.
    /// </summary>
    public JobStep Step { get; }

    /// <summary>
    /// Reads the <c>Run</c> method of the given step type. Only the signature itself is judged
    /// here; whether its parameters can be satisfied depends on the job, judged by the rules.
    /// </summary>
    /// <param name="stepType">The step type to read.</param>
    public static Result<StepDescriptor> Describe(Type stepType)
    {
        if (stepType.GetCustomAttribute<StepAttribute>() is not { } metadata)
        {
            return Result.Error($"{stepType.Name} must declare a [Step] attribute naming and classifying it.");
        }

        var runs = stepType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == "Run")
            .ToList();
        if (runs.Count != 1)
        {
            return Result.Error($"{stepType.Name} must declare exactly one public Run method.");
        }

        var run = runs[0];
        var returned = run.ReturnType;
        Type? produces = null;
        if (returned.IsGenericType && returned.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var payload = returned.GetGenericArguments()[0];
            if (payload.IsGenericType && payload.GetGenericTypeDefinition() == typeof(StepResult<>))
            {
                produces = payload.GetGenericArguments()[0];
            }
            else if (payload != typeof(StepResult))
            {
                returned = typeof(void);
            }
        }
        else
        {
            returned = typeof(void);
        }

        if (returned == typeof(void))
        {
            return Result.Error($"{stepType.Name}.Run must return Task<StepResult> or Task<StepResult<T>>.");
        }

        var nullability = new NullabilityInfoContext();
        var parameters = run.GetParameters()
            .Select(parameter => (parameter.ParameterType, Classify(parameter, nullability)))
            .ToList();

        return new StepDescriptor(stepType, metadata, run, produces, parameters);
    }


    /// <summary>
    /// Runs the step, supplying its parameters from state and services, and storing what it
    /// produces for the steps after it.
    /// </summary>
    /// <param name="step">The resolved step instance.</param>
    /// <param name="services">The service provider for parameters no step produces.</param>
    /// <param name="state">The pipeline state for consumed and produced values.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Invoke(object step, IServiceProvider services, Dictionary<Type, object> state, CancellationToken cancellationToken)
    {
        var arguments = new object?[_parameters.Count];
        for (var i = 0; i < _parameters.Count; i++)
        {
            var (type, source) = _parameters[i];
            arguments[i] = source switch
            {
                ParameterSource.Token => cancellationToken,
                ParameterSource.Optional => state.GetValueOrDefault(type),
                _ => state.GetValueOrDefault(type) ?? services.GetService(type)
                    ?? throw new InvalidOperationException($"No {type.Name} in pipeline state or services; an earlier step should have produced it.")
            };
        }

        Task task;
        try
        {
            task = (Task)_run.Invoke(step, arguments)!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }

        await task;

        var result = _taskResult.GetValue(task)!;
        if (result is IProducedResult production)
        {
            if (production.Value is { } value)
            {
                state[Produces!] = value;
            }

            return production.Outcome;
        }

        return (StepResult)result;
    }

    private static ParameterSource Classify(ParameterInfo parameter, NullabilityInfoContext nullability)
    {
        var type = parameter.ParameterType;
        if (type == typeof(CancellationToken))
        {
            return ParameterSource.Token;
        }

        // A nullable parameter is an optional read of produced state: present when some earlier
        // step produced it, null otherwise.
        if (!type.IsValueType && nullability.Create(parameter).ReadState == NullabilityState.Nullable)
        {
            return ParameterSource.Optional;
        }

        return ParameterSource.Required;
    }
}
