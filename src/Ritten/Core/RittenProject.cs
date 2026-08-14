using System.Text.Json;

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

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
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
    /// <exception cref="JsonException">The file contains a key the pipeline doesn't recognize.</exception>
    public TSettings GetSettings<TSettings>() => Settings.Deserialize<TSettings>(_serializerOptions)
                                                 ?? throw new InvalidOperationException("Unable to read project settings.");

    /// <summary>
    /// Walks up from the given directory looking for a project, or returns <c>null</c> if there
    /// isn't one.
    /// </summary>
    /// <exception cref="JsonException">The project file is not valid JSON.</exception>
    public static async Task<RittenProject?> Resolve(string directory)
    {
        var current = new DirectoryInfo(Path.GetFullPath(directory));
        while (current is not null)
        {
            var path = Path.Combine(current.FullName, FileName);
            if (File.Exists(path))
            {
                await using var stream = File.OpenRead(path);
                var settings = await JsonSerializer.DeserializeAsync<JsonElement>(stream, _serializerOptions);

                return new RittenProject { Directory = current.FullName, Settings = settings };
            }

            current = current.Parent;
        }

        return null;
    }
}
