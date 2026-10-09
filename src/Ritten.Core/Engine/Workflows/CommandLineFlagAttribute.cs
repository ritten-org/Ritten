namespace Ritten.Engine.Workflows;

/// <summary>
/// Reads an arguments property from a command-line flag, e.g. <c>--force</c>, rather than from the project file.
/// </summary>
/// <remarks>
/// A flag that is given is read as the JSON value <c>true</c>, through the property type's converter; one that
/// isn't leaves the property's default.
/// </remarks>
/// <param name="name">The flag's name, without the leading dashes.</param>
/// <param name="description">What the flag changes, as help text.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CommandLineFlagAttribute(string name, string description) : Attribute
{
    /// <summary>
    /// The flag's name, without the leading dashes.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// What the flag changes, as help text.
    /// </summary>
    public string Description { get; } = description;

    /// <summary>
    /// A shorter spelling of the flag, with its dash, e.g. <c>-f</c>.
    /// </summary>
    public string? Alias { get; init; }
}