using System.Text.RegularExpressions;
using Ritten.Engine;

namespace Ritten.Commands;

/// <summary>
/// Represents the name of an environment variable.
/// </summary>
public sealed partial record EnvironmentVariableName : IComparable<EnvironmentVariableName>
{
    private EnvironmentVariableName(string value) => Value = value;

    /// <summary>
    /// The name as the process sees it.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// The name, or an error saying why it is not one: for a name read from somewhere, such as an env file.
    /// </summary>
    public static Result<EnvironmentVariableName> Parse(string value) =>
        IsName(value) ? new EnvironmentVariableName(value) : new Error($"'{value}' is not an environment variable name: a letter or underscore, then letters, digits and underscores.");

    /// <summary>
    /// The name written in code; one that is not a name is a programming error.
    /// </summary>
    /// <exception cref="ArgumentException">The value is not a name.</exception>
    public static EnvironmentVariableName From(string value) =>
        IsName(value) ? new EnvironmentVariableName(value) : throw new ArgumentException(Parse(value).Errors?.FirstOrDefault()?.Message, nameof(value));

    /// <inheritdoc />
    public int CompareTo(EnvironmentVariableName? other) => string.CompareOrdinal(Value, other?.Value);

    /// <inheritdoc />
    public override string ToString() => Value;

    private static bool IsName(string value) => value is { Length: > 0 } && Pattern().IsMatch(value);

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex Pattern();
}
