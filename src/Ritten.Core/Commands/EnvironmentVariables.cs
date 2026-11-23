using System.Collections;
using System.Collections.Immutable;

namespace Ritten.Commands;

/// <summary>
/// Represents an immutable set of environment variables.
/// </summary>
public sealed class EnvironmentVariables : IReadOnlyDictionary<EnvironmentVariableName, string>, IEquatable<EnvironmentVariables>
{
    private readonly ImmutableSortedDictionary<EnvironmentVariableName, string> _variables;

    private EnvironmentVariables(ImmutableSortedDictionary<EnvironmentVariableName, string> variables) => _variables = variables;

    /// <summary>
    /// No variables.
    /// </summary>
    public static EnvironmentVariables Empty { get; } = new(ImmutableSortedDictionary<EnvironmentVariableName, string>.Empty);

    /// <summary>
    /// The given variables; where a name is given twice, the later value.
    /// </summary>
    public static EnvironmentVariables From(IEnumerable<KeyValuePair<EnvironmentVariableName, string>> variables) =>
        variables.Aggregate(Empty, (set, variable) => set.With(variable.Key, variable.Value));

    /// <summary>
    /// The given variables, named in code; a name that is not one is a programming error.
    /// </summary>
    /// <exception cref="ArgumentException">A name is not an environment variable name.</exception>
    public static EnvironmentVariables From(IEnumerable<KeyValuePair<string, string>> variables) =>
        From(variables.Select(variable => KeyValuePair.Create(EnvironmentVariableName.From(variable.Key), variable.Value)));

    /// <summary>
    /// These variables with <paramref name="name"/> set to <paramref name="value"/>, replacing any value it had.
    /// </summary>
    public EnvironmentVariables With(EnvironmentVariableName name, string value) => new(_variables.SetItem(name, value));

    /// <summary>
    /// These variables without <paramref name="name"/>.
    /// </summary>
    public EnvironmentVariables Without(EnvironmentVariableName name) => new(_variables.Remove(name));

    /// <summary>
    /// These variables and <paramref name="later"/>'s, a value of <paramref name="later"/>'s replacing one of these.
    /// </summary>
    public EnvironmentVariables Merge(EnvironmentVariables later) => later.Count == 0 ? this : Count == 0 ? later : new(_variables.SetItems(later._variables));

    /// <summary>
    /// <paramref name="earlier"/>'s variables and <paramref name="later"/>'s, a value of <paramref name="later"/>'s
    /// replacing one of <paramref name="earlier"/>'s: <see cref="Merge"/>.
    /// </summary>
    public static EnvironmentVariables operator +(EnvironmentVariables earlier, EnvironmentVariables later) => earlier.Merge(later);

    /// <inheritdoc />
    public string this[EnvironmentVariableName key] => _variables[key];

    /// <inheritdoc />
    public IEnumerable<EnvironmentVariableName> Keys => _variables.Keys;

    /// <inheritdoc />
    public IEnumerable<string> Values => _variables.Values;

    /// <inheritdoc />
    public int Count => _variables.Count;

    /// <inheritdoc />
    public bool ContainsKey(EnvironmentVariableName key) => _variables.ContainsKey(key);

    /// <inheritdoc />
    public bool TryGetValue(EnvironmentVariableName key, out string value)
    {
        if (_variables.TryGetValue(key, out var found))
        {
            value = found;
            return true;
        }

        value = string.Empty;
        return false;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<EnvironmentVariableName, string>> GetEnumerator() => _variables.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public bool Equals(EnvironmentVariables? other) =>
        other is not null && Count == other.Count && _variables.All(variable => other.TryGetValue(variable.Key, out var value) && value == variable.Value);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as EnvironmentVariables);

    /// <inheritdoc />
    public override int GetHashCode() => _variables.Aggregate(Count, (hash, variable) => HashCode.Combine(hash, variable.Key, variable.Value));

    /// <summary>
    /// The variables' names, never their values, which can be secrets.
    /// </summary>
    public override string ToString() => $"[{string.Join(", ", Keys)}]";
}
