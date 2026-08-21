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
    public abstract TResult Convert<TResult>(IJobArgumentConverter<TResult> converter);

    /// <summary>
    /// Declares a value of a type the front end can read for itself.
    /// </summary>
    /// <typeparam name="T">The type the value reads as.</typeparam>
    /// <param name="name">The name the value is supplied under.</param>
    /// <param name="description">What the value is for, as help text.</param>
    /// <param name="alias">A short alternative spelling, when the argument has one.</param>
    /// <param name="required">Whether the job cannot run without it.</param>
    public static JobArgument<T> Value<T>(
        string name,
        string description,
        string? alias = null,
        bool required = false
    ) => new(name, description, null, alias, required);

    /// <summary>
    /// Declares a value that requires a custom parser.
    /// </summary>
    /// <typeparam name="T">The type the text reads as.</typeparam>
    /// <param name="name">The name the value is supplied under.</param>
    /// <param name="description">What the value is for, as help text.</param>
    /// <param name="parse">Parses the argument.</param>
    /// <param name="alias">A short alternative spelling, when the argument has one.</param>
    /// <param name="required">Whether the job cannot run without it.</param>
    public static JobArgument<T> Value<T>(
        string name,
        string description,
        Func<string, Result<T>> parse,
        string? alias = null,
        bool required = false
    ) => new(name, description, parse, alias, required);
}

/// <summary>
/// A value a job takes from whoever invokes it, of a known type.
/// </summary>
/// <typeparam name="T">The type the supplied text reads as.</typeparam>
public sealed class JobArgument<T> : JobArgument
{
    internal JobArgument(string name, string description, Func<string, Result<T>>? parse, string? alias, bool required)
        : base(name, description, alias, required) => Parse = parse;

    /// <summary>
    /// Reads supplied text into the declared type, or <c>null</c> when the front end reads it.
    /// </summary>
    public Func<string, Result<T>>? Parse { get; }

    /// <inheritdoc />
    public override TResult Convert<TResult>(IJobArgumentConverter<TResult> converter) => converter.Convert(this);
}
