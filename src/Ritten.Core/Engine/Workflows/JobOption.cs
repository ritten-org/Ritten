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
    /// Sets what the command line gave for the option on a job's arguments.
    /// </summary>
    /// <remarks>
    /// A flag that wasn't given, or a value option left out, leaves the property as it was.
    /// </remarks>
    /// <param name="arguments">The arguments to set the value on, of the job's arguments type.</param>
    /// <param name="given">What the command line gave: whether a flag was given, or the value an option read.</param>
    /// <returns>Why the value couldn't be set; empty once it is.</returns>
    public IReadOnlyList<Error> Set(object arguments, object? given)
    {
        var value = IsFlag
            ? given is true ? Read(JsonValue.Create(true)) : null
            : given is null ? null : new Result<object>(given);
        if (value is { IsError: true })
        {
            return value.Errors;
        }

        if (value?.Value is { } set)
        {
            Property.SetValue(arguments, set);
        }

        return [];
    }

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
            // The serializer names the type, which means nothing to whoever typed the value.
            var type = Nullable.GetUnderlyingType(Property.PropertyType) ?? Property.PropertyType;
            return type.IsEnum
                ? Result.Error($"'--{Name}' takes {Choices(type)}, not '{value}'.", exception)
                : Result.Error(exception.Message, exception);
        }
    }

    private static string Choices(Type type)
    {
        var names = Enum.GetNames(type).Select(JsonNamingPolicy.CamelCase.ConvertName).ToList();
        return names.Count == 1 ? names[0] : $"{string.Join(", ", names[..^1])} or {names[^1]}";
    }
}
