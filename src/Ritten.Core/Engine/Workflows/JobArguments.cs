namespace Ritten.Engine.Workflows;

/// <summary>
/// What a run was invoked with, for the inputs its job declared.
/// </summary>
public sealed class JobArguments
{
    private readonly IReadOnlyDictionary<JobArgument, object?> _values;

    /// <summary>
    /// Creates a new instance of the <see cref="JobArguments"/>.
    /// </summary>
    /// <param name="values">The read values, keyed by the declaration they were read for.</param>
    internal JobArguments(IReadOnlyDictionary<JobArgument, object?>? values = null) =>
        _values = values ?? new Dictionary<JobArgument, object?>();

    /// <summary>
    /// An empty set, for a job that declares no inputs.
    /// </summary>
    public static JobArguments None { get; } = new();

    /// <summary>
    /// Reads the value supplied for an input, or <c>null</c> when the caller supplied none.
    /// </summary>
    /// <typeparam name="T">The type the input reads as.</typeparam>
    /// <param name="argument">The declaration to read, as the job declared it.</param>
    public T? Get<T>(JobArgument<T> argument) where T : class =>
        _values.TryGetValue(argument, out var value) ? value as T : null;

    /// <summary>
    /// The declarations a value was supplied for.
    /// </summary>
    public IReadOnlyCollection<JobArgument> Arguments => [.. _values.Keys];
}
