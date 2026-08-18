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

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
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
    internal JsonElement Settings { get; init; }

    /// <summary>
    /// The path of the project's <c>ritten.json</c>, for error messages.
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
    /// <param name="ct">Cancellation token.</param>
    public static async Task<Result<RittenProject>> Resolve(string directory, CancellationToken ct)
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
                    var settings = await JsonSerializer.DeserializeAsync<JsonElement>(stream, SerializerOptions, ct);

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
