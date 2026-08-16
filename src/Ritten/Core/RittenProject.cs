using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ritten.Core;

/// <summary>
/// A located Ritten build project: where it is, and the settings it declares.
/// </summary>
internal sealed class RittenProject
{
    /// <summary>
    /// The configuration file that marks the root of a project.
    /// </summary>
    public const string FileName = "ritten.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNameCaseInsensitive = true,
        RespectNullableAnnotations = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// The directory containing the project's <c>ritten.json</c>.
    /// </summary>
    public required string Directory { get; init; }

    /// <summary>
    /// The raw contents of the project's <c>ritten.json</c>.
    /// </summary>
    private JsonElement Settings { get; init; }

    /// <summary>
    /// The path of the project's <c>ritten.json</c>, for error messages.
    /// </summary>
    public string FilePath => Path.Combine(Directory, FileName);

    /// <summary>
    /// Reads the project's settings as the given pipeline's settings type.
    /// </summary>
    /// <typeparam name="TSettings">The pipeline's settings type.</typeparam>
    public Result<TSettings> GetSettings<TSettings>() where TSettings : PipelineSettings
    {
        try
        {
            if (Settings.Deserialize<TSettings>(SerializerOptions) is not { } settings)
            {
                return Result.Error($"'{FilePath}' is empty.");
            }

            return settings;
        }
        catch (JsonException exception)
        {
            return Result.Error($"Could not read '{FilePath}': {exception.Message}", exception);
        }
    }

    /// <summary>
    /// Walks up from the given directory looking for a project.
    /// </summary>
    /// <param name="directory">The directory to start from, usually the working directory.</param>
    public static async Task<Result<RittenProject>> Resolve(string directory)
    {
        var current = new DirectoryInfo(Path.GetFullPath(directory));
        while (current is not null)
        {
            var path = Path.Combine(current.FullName, FileName);
            if (File.Exists(path))
            {
                try
                {
                    await using var stream = File.OpenRead(path);
                    var settings = await JsonSerializer.DeserializeAsync<JsonElement>(stream, SerializerOptions);

                    return new RittenProject { Directory = current.FullName, Settings = settings };
                }
                catch (JsonException exception)
                {
                    return Result.Error($"Could not read '{path}': {exception.Message}", exception);
                }
            }

            current = current.Parent;
        }

        return Result.Error($"No {FileName} found in '{Path.GetFullPath(directory)}' or any parent directory.");
    }
}
