using System.Text.Json;

namespace Ritten.Core;

/// <summary>
/// A located Ritten build project: where it is, and the settings it declares.
/// </summary>
internal sealed class RittenProject
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
    /// The path of the project file, for error messages.
    /// </summary>
    public string FilePath => Path.Combine(Directory, FileName);

    /// <summary>
    /// Reads which workflow the settings declare.
    /// </summary>
    public Result<string> GetWorkflowName()
    {
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
    /// Walks up from the given directory looking for a project.
    /// </summary>
    /// <param name="directory">The directory to start from, usually the working directory.</param>
    /// <param name="fileName">The name of the file that marks a project's root.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<Result<RittenProject>> Resolve(string directory, string fileName, CancellationToken ct)
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

        return Result.Error($"No {fileName} found in '{Path.GetFullPath(directory)}' or any parent directory.");
    }
}
