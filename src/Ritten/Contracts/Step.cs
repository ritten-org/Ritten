using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Ritten.Contracts;

/// <summary>
/// A step of a declared job.
/// </summary>
/// <param name="name">The step's display name.</param>
/// <param name="kind">What the step's outcome means.</param>
/// <param name="produces">The type the step produces, or <c>null</c> for a non-producing step.</param>
/// <param name="requires">The parameter types the step cannot run without.</param>
public sealed class Step(string name, StepKind kind, Type? produces, IReadOnlyList<Type> requires)
{
    private enum ParameterSource
    {
        Token,
        Optional,
        Required
    }

    private readonly MethodInfo? _run;
    private readonly bool _asynchronous;
    private readonly PropertyInfo? _taskResult;
    private readonly IReadOnlyList<(Type Type, ParameterSource Source)> _parameters = [];

    private Step(Type stepType, StepAttribute metadata, MethodInfo run, bool asynchronous, Type? produces, IReadOnlyList<(Type Type, ParameterSource Source)> parameters)
        : this(metadata.Name, metadata.Kind, produces, [.. parameters.Where(p => p.Source == ParameterSource.Required).Select(p => p.Type)])
    {
        StepType = stepType;
        _run = run;
        _asynchronous = asynchronous;
        _parameters = parameters;
        _taskResult = asynchronous ? run.ReturnType.GetProperty(nameof(Task<>.Result)) : null;
    }

    /// <summary>
    /// The step's display name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// What the step's outcome means.
    /// </summary>
    public StepKind Kind { get; } = kind;

    /// <summary>
    /// The type the step produces into workflow state, or <c>null</c> for a non-producing step.
    /// </summary>
    public Type? Produces { get; } = produces;

    /// <summary>
    /// The parameter types the step cannot run without.
    /// </summary>
    public IReadOnlyList<Type> Requires { get; } = requires;

    /// <summary>
    /// The type declaring the <c>Run</c> method, registered for the container to construct.
    /// </summary>
    internal Type StepType => field ?? throw new InvalidOperationException($"'{Name}' was built from its facts alone; only a step described from its type can run.");

    /// <summary>
    /// Reads a step from its type.
    /// </summary>
    /// <typeparam name="TStep">The step type to read.</typeparam>
    public static Step FromType<TStep>() where TStep : class => FromType(typeof(TStep));

    /// <summary>
    /// Reads a step from its type.
    /// </summary>
    /// <param name="stepType">The step type to read.</param>
    internal static Step FromType(Type stepType)
    {
        if (stepType.GetCustomAttribute<StepAttribute>() is not { } metadata)
        {
            throw new InvalidOperationException($"{stepType.Name} must declare a [Step] attribute naming and classifying it.");
        }

        var runs = stepType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == "Run")
            .ToList();
        if (runs.Count != 1)
        {
            throw new InvalidOperationException($"{stepType.Name} must declare exactly one public Run method.");
        }

        // A step with nothing to await returns its result directly, like a minimal API handler.
        var run = runs[0];
        var payload = run.ReturnType;
        var asynchronous = payload.IsGenericType && payload.GetGenericTypeDefinition() == typeof(Task<>);
        if (asynchronous)
        {
            payload = payload.GetGenericArguments()[0];
        }

        Type? produces = null;
        if (payload.IsGenericType && payload.GetGenericTypeDefinition() == typeof(StepResult<>))
        {
            produces = payload.GetGenericArguments()[0];
        }
        else if (payload != typeof(StepResult))
        {
            throw new InvalidOperationException($"{stepType.Name}.Run must return StepResult, StepResult<T>, Task<StepResult>, or Task<StepResult<T>>.");
        }

        var nullability = new NullabilityInfoContext();
        var parameters = run.GetParameters()
            .Select(parameter => (parameter.ParameterType, Classify(parameter, nullability)))
            .ToList();

        return new Step(stepType, metadata, run, asynchronous, produces, parameters);
    }

    /// <summary>
    /// Runs the step, supplying its parameters from workflow state.
    /// </summary>
    /// <param name="step">The resolved step instance.</param>
    /// <param name="state">The workflow state for consumed and produced values.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    internal async Task<StepResult> Invoke(object step, Dictionary<Type, object> state, CancellationToken cancellationToken)
    {
        var arguments = new object?[_parameters.Count];
        for (var i = 0; i < _parameters.Count; i++)
        {
            var (type, source) = _parameters[i];
            arguments[i] = source switch
            {
                ParameterSource.Token => cancellationToken,
                ParameterSource.Optional => state.GetValueOrDefault(type),
                _ => state.GetValueOrDefault(type)
                    ?? throw new InvalidOperationException($"No {type.Name} in workflow state; an earlier step should have produced it.")
            };
        }

        object result;
        try
        {
            // The runner resolves the instance by StepType, which throws first for a step that
            // was never described — by here _run is certain to exist.
            result = _run!.Invoke(step, arguments)!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }

        if (_asynchronous)
        {
            var task = (Task)result;
            await task;
            result = _taskResult!.GetValue(task)!;
        }

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
