using System.Text.Json;

namespace Ritten.Engine;

/// <summary>
/// A located Ritten build project: where it is, and the settings it declares.
/// </summary>
public sealed class RittenProject
{
    /// <summary>
    /// The configuration file that marks the root of a project, unless the host renames it.
    /// </summary>
    public const string DefaultFileName = "ritten.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// The directory containing the project file.
    /// </summary>
    public required string Directory { get; init; }

    /// <summary>
    /// The name of the project file, as the host declared it.
    /// </summary>
    public string FileName { get; init; } = DefaultFileName;

    /// <summary>
    /// The raw contents of the project file.
    /// </summary>
    internal JsonElement Settings { get; init; }

    /// <summary>
    /// Whether the project file was created in memory rather than loaded from a file.
    /// </summary>
    public bool IsSynthetic { get; private init; }

    /// <summary>
    /// The path of the project file, for error messages.
    /// </summary>
    public string FilePath => Path.Combine(Directory, FileName);

    /// <summary>
    /// The project a repository would have, for a directory that hasn't got one yet.
    /// </summary>
    /// <param name="directory">The directory the project file would live in.</param>
    /// <param name="fileName">The name of the file that marks a project's root.</param>
    public static RittenProject Synthetic(string directory, string fileName) => new()
    {
        Directory = Path.GetFullPath(directory),
        FileName = fileName,
        IsSynthetic = true,

        // An empty document, so every setting reads as its default rather than failing to read.
        Settings = JsonDocument.Parse("{}").RootElement
    };

    /// <summary>
    /// Reads which workflow the settings declare. s
    /// </summary>
    public Result<string> GetWorkflowName()
    {
        if (IsSynthetic)
        {
            return Result.Error($"No {FileName} found in '{Directory}' or any parent directory.");
        }

        if (Settings.TryGetProperty("workflow", out var workflowProp))
        {
            var workflow = workflowProp.GetString();
            if (workflow is not null)
            {
                return workflow;
            }
        }
        return Result.Error($"'{FilePath}' does not declare which workflow it runs; set \"workflow\".");
    }

    /// <summary>
    /// Walks up from the given directory looking for a project. A directory
    /// with no project file resolves to a <see cref="Synthetic"/> one.
    /// </summary>
    /// <param name="directory">The directory to start from, usually the working directory.</param>
    /// <param name="fileName">The name of the file that marks a project's root.</param>
    /// <param name="ct">Cancellation token.</param>
    internal static async Task<Result<RittenProject>> Resolve(string directory, string fileName, CancellationToken ct)
    {
        var current = new DirectoryInfo(Path.GetFullPath(directory));
        while (current is not null)
        {
            var path = Path.Combine(current.FullName, fileName);
            if (File.Exists(path))
            {
                try
                {
                    await using var stream = File.OpenRead(path);
                    var settings = await JsonSerializer.DeserializeAsync<JsonElement>(stream, SerializerOptions, ct);

                    return new RittenProject { Directory = current.FullName, FileName = fileName, Settings = settings };
                }
                catch (JsonException exception)
                {
                    return Result.Error($"Could not read '{path}': {exception.Message}", exception);
                }
            }

            current = current.Parent;
        }

        return Synthetic(directory, fileName);
    }
}
