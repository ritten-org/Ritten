namespace Ritten.Engine.Workflows;

/// <summary>
/// Reads an arguments property from a command-line option that takes a value, e.g. <c>--version 1.2.0</c>,
/// rather than from the project file.
/// </summary>
/// <remarks>
/// The value is read as the project file's values are: as a JSON string, through the property type's converter,
/// so a bad value is refused in the domain's own words.
/// </remarks>
/// <param name="name">The option's name, without the leading dashes.</param>
/// <param name="description">What the option asks for, as help text.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CommandLineOptionAttribute(string name, string description) : Attribute
{
    /// <summary>
    /// The option's name, without the leading dashes.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// What the option asks for, as help text.
    /// </summary>
    public string Description { get; } = description;

    /// <summary>
    /// A shorter spelling of the option, with its dash, e.g. <c>-f</c>.
    /// </summary>
    public string? Alias { get; init; }
}