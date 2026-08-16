using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ritten.Core;

/// <summary>
/// The settings every pipeline's <c>ritten.json</c> shares.
/// </summary>
public abstract record PipelineSettings
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        RespectNullableAnnotations = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Reads the given project's settings as the given shape.
    /// </summary>
    /// <typeparam name="TSettings">The settings shape to read.</typeparam>
    /// <param name="project">The project whose settings to read.</param>
    internal static Result<TSettings> Read<TSettings>(RittenProject project) where TSettings : PipelineSettings
    {
        try
        {
            if (project.Settings.Deserialize<TSettings>(SerializerOptions) is not { } settings)
            {
                return Result.Error($"'{project.FilePath}' is empty.");
            }

            return settings;
        }
        catch (JsonException exception)
        {
            return Result.Error($"Could not read '{project.FilePath}': {exception.Message}", exception);
        }
    }
}
