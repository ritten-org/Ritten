namespace Ritten.Engine.Workflows;

/// <summary>
/// A value a job takes from whoever invokes it.
/// </summary>
public abstract class JobArgument
{
    /// <summary>
    /// Creates a new instance of the <see cref="JobArgument"/>.
    /// </summary>
    /// <param name="name">The name the value is supplied under.</param>
    /// <param name="description">What the value is for, as help text.</param>
    /// <param name="alias">A short alternative spelling, when the input has one.</param>
    /// <param name="required">Whether the job cannot run without it.</param>
    private protected JobArgument(string name, string description, string? alias, bool required)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        Name = name;
        Description = description;
        Alias = alias;
        Required = required;
    }

    /// <summary>
    /// The name the value is supplied under.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// What the value is for, as help text.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// A short alternative spelling, when the input has one.
    /// </summary>
    public string? Alias { get; }

    /// <summary>
    /// Whether the job cannot run without it. A flag is never required — its absence is an answer.
    /// </summary>
    public bool Required { get; }

    /// <summary>
    /// Maps this declaration through the given mapper, which recovers the type it reads as.
    /// </summary>
    /// <typeparam name="TResult">What the declaration maps to.</typeparam>
    /// <param name="converter">The mapper to dispatch to.</param>
    public abstract TResult Map<TResult>(Func<JobArgument<TResult>, TResult> converter);

    /// <summary>
    /// Declares a value the caller supplies as text, read by the domain that named it.
    /// </summary>
    /// <typeparam name="T">The type the text reads as.</typeparam>
    /// <param name="name">The name the value is supplied under.</param>
    /// <param name="description">What the value is for, as help text.</param>
    /// <param name="read">Reads the text, reporting in the domain's own words what a bad one is.</param>
    /// <param name="alias">A short alternative spelling, when the input has one.</param>
    /// <param name="required">Whether the job cannot run without it.</param>
    public static JobArgument<T> Value<T>(
        string name,
        string description,
        Func<string, Result<T>> read,
        string? alias = null,
        bool required = false
    ) => new(name, description, read, alias, required);
}

/// <summary>
/// A value a job takes from whoever invokes it, of a known type.
/// </summary>
/// <typeparam name="T">The type the supplied text reads as.</typeparam>
public sealed class JobArgument<T> : JobArgument
{
    private readonly Func<string, Result<T>> _read;

    internal JobArgument(string name, string description, Func<string, Result<T>> read, string? alias, bool required)
        : base(name, description, alias, required) => _read = read;

    /// <inheritdoc />
    public override TResult Map<TResult>(Func<JobArgument<TResult>, TResult> converter) => converter((JobArgument<TResult>)this);

    /// <summary>
    /// Reads supplied text into the declared type, in the words of the domain that declared it.
    /// </summary>
    /// <param name="text">The text the caller supplied.</param>
    public Result<T> Read(string text) => _read(text);
}
