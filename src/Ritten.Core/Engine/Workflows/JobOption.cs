using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ritten.Engine.Workflows;

/// <summary>
/// A value a job takes from the command line, declared by a <see cref="CommandLineOptionAttribute"/> or
/// <see cref="CommandLineFlagAttribute"/> on its arguments model.
/// </summary>
public sealed class JobOption
{
    internal JobOption(PropertyInfo property, string name, string description, string? alias, bool isFlag)
    {
        Property = property;
        Name = name;
        Description = description;
        Alias = alias;
        IsFlag = isFlag;
    }

    /// <summary>
    /// The option's name, without the leading dashes.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// What the option asks for, as help text.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// A shorter spelling of the option, with its dash.
    /// </summary>
    public string? Alias { get; }

    /// <summary>
    /// Whether the option is a flag, given or not, rather than one that takes a value.
    /// </summary>
    public bool IsFlag { get; }

    /// <summary>
    /// The arguments property the option sets.
    /// </summary>
    internal PropertyInfo Property { get; }

    /// <summary>
    /// Reads the text given for a value option into the arguments property's type.
    /// </summary>
    /// <param name="text">The text given on the command line.</param>
    public Result<object> Read(string text) => Read(JsonValue.Create(text));

    /// <summary>
    /// Reads a given flag into the arguments property's type.
    /// </summary>
    internal Result<object> ReadFlag() => Read(JsonValue.Create(true));

    /// <summary>
    /// Sets the value on a freshly read arguments model.
    /// </summary>
    internal void Set(object arguments, object value) => Property.SetValue(arguments, value);

    private Result<object> Read(JsonNode value)
    {
        try
        {
            return value.Deserialize(Property.PropertyType, ArgumentsModel.SerializerOptions) is { } read
                ? new Result<object>(read)
                : Result.Error($"'--{Name}' needs a value.");
        }
        catch (JsonException exception)
        {
            return Result.Error(exception.Message, exception);
        }
    }
}
