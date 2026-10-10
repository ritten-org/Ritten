using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Ritten.Engine.Workflows;

/// <summary>
/// What an arguments type declares: the values it puts into a run's state, and the ones it reads from the
/// command line rather than the project file.
/// </summary>
internal sealed class ArgumentsModel
{
    private static readonly ConcurrentDictionary<Type, ArgumentsModel> Models = new();

    private ArgumentsModel(Type type)
    {
        Type = type;
        var nullability = new NullabilityInfoContext();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && p.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .ToList();

        Inputs = [.. properties.Select(p => new ArgumentsInput(p, Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType, IsOptional(p, nullability)))];
        Options = [.. properties.Select(Option).OfType<JobOption>()];
    }

    /// <summary>
    /// How a project file and the values read from it are read, so files written for any version of the tool read the same.
    /// </summary>
    internal static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        RespectNullableAnnotations = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// The arguments type.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Every property, each put into the run's state under its own type.
    /// </summary>
    public IReadOnlyList<ArgumentsInput> Inputs { get; }

    /// <summary>
    /// The properties read from the command line.
    /// </summary>
    public IReadOnlyList<JobOption> Options { get; }

    /// <summary>
    /// The model for the given arguments type, read once.
    /// </summary>
    public static ArgumentsModel For(Type type) => Models.GetOrAdd(type, t => new ArgumentsModel(t));

    /// <summary>
    /// What makes the arguments type unusable, judged when the application is built.
    /// </summary>
    public IEnumerable<Error> Validate()
    {
        foreach (var group in Inputs.GroupBy(i => i.Type).Where(g => g.Count() > 1))
        {
            yield return Result.Error(
                $"{Type.Name} has several properties of type {group.Key.Name} ({string.Join(", ", group.Select(i => i.Property.Name))}); " +
                "steps receive arguments by type, so each needs a type of its own.");
        }

        foreach (var input in Inputs.Where(i => IsScalar(i.Type)))
        {
            yield return Result.Error(
                $"{Type.Name}.{input.Property.Name} is a {input.Type.Name}; give it a type that says what it is, since steps receive it by type.");
        }

        foreach (var input in Inputs.Where(i => IsOption(i.Property) && i.Property.GetCustomAttribute<System.Runtime.CompilerServices.RequiredMemberAttribute>() is not null))
        {
            yield return Result.Error($"{Type.Name}.{input.Property.Name} is read from the command line, so it can't be required; give it a default.");
        }

        foreach (var input in Inputs.Where(i => IsOption(i.Property) && i.Property.SetMethod is null))
        {
            yield return Result.Error($"{Type.Name}.{input.Property.Name} is read from the command line, so it needs a setter or init accessor.");
        }
    }

    /// <summary>
    /// Reads the arguments from the project file, then sets the values given on the command line.
    /// </summary>
    /// <param name="project">The project whose file holds every value not read from the command line.</param>
    /// <param name="options">The values given on the command line, already read, keyed by the option; a flag maps to whether it was given.</param>
    public Result<object> Read(RittenProject project, IReadOnlyDictionary<JobOption, object?> options)
    {
        object arguments;
        try
        {
            // The command line owns its values, so the file can't set them.
            // The element keeps the file's own text, comments and trailing commas included.
            var file = JsonNode.Parse(project.Settings.GetRawText(), documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
            if (file is JsonObject values)
            {
                foreach (var option in Options)
                {
                    values.Remove(JsonNamingPolicy.CamelCase.ConvertName(option.Property.Name));
                }
            }

            if (file.Deserialize(Type, SerializerOptions) is not { } read)
            {
                return Result.Error($"'{project.FilePath}' is empty.");
            }

            arguments = read;
        }
        catch (JsonException exception)
        {
            return Result.Error($"Could not read '{project.FilePath}': {exception.Message}", exception);
        }

        List<Error> errors = [];
        foreach (var (option, given) in options)
        {
            errors.AddRange(option.Set(arguments, given));
        }

        return errors.Count > 0 ? errors : new Result<object>(arguments);
    }

    /// <summary>
    /// Puts every value the arguments hold into the run's state, under the property's type.
    /// </summary>
    public void Seed(object arguments, IDictionary<Type, object> state)
    {
        foreach (var input in Inputs)
        {
            if (input.Property.GetValue(arguments) is { } value)
            {
                state[input.Type] = value;
            }
        }
    }

    private static JobOption? Option(PropertyInfo property) =>
        property.GetCustomAttribute<CommandLineOptionAttribute>() is { } option
            ? new JobOption(property, option.Name, option.Description, option.Alias, isFlag: false)
            : property.GetCustomAttribute<CommandLineFlagAttribute>() is { } flag
                ? new JobOption(property, flag.Name, flag.Description, flag.Alias, isFlag: true)
                : null;

    private static bool IsOption(PropertyInfo property) =>
        property.GetCustomAttribute<CommandLineOptionAttribute>() is not null || property.GetCustomAttribute<CommandLineFlagAttribute>() is not null;

    // Only a reference type can be told apart from its absence; a value type always holds something.
    private static bool IsOptional(PropertyInfo property, NullabilityInfoContext nullability) =>
        Nullable.GetUnderlyingType(property.PropertyType) is not null
        || (!property.PropertyType.IsValueType && nullability.Create(property).ReadState == NullabilityState.Nullable);

    // A scalar says nothing about what it holds, so two steps could never agree on which one they mean.
    private static bool IsScalar(Type type) => type.IsPrimitive || type == typeof(string) || type == typeof(decimal);
}
