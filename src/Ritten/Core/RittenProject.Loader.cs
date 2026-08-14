using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ritten.Core;

internal partial class RittenProject
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNameCaseInsensitive = true,
        RespectNullableAnnotations =  true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// The configuration file that marks the root of a project.
    /// </summary>
    public const string FileName = "ritten.json";

    /// <summary>
    /// Resolves the Ritten project from the given directory.
    /// </summary>
    public static async Task<RittenProject?> Resolve(string directory)
    {
        var current = new DirectoryInfo(Path.GetFullPath(directory));
        while (current is not null)
        {
            var projectFile = Path.Combine(current.FullName, FileName);
            if (File.Exists(projectFile))
            {
                await using var stream = File.OpenRead(projectFile);
                var file = await JsonSerializer.DeserializeAsync<RittenProjectFile>(stream, _serializerOptions)
                           ?? throw new InvalidOperationException($"Unable to read project at '${projectFile}'.");
                return FromFile(file, current.FullName);
            }

            current = current.Parent;
        }

        return null;
    }

    private static RittenProject FromFile(RittenProjectFile file, string directory)
    {
        return new RittenProject
        {
            Project = file.Project,
            Directory = directory,
            Configuration = file.Configuration,
            Changelog = file.Changelog,
            Repository = file.Repository,
            TagPrefix = file.TagPrefix,
            Feed = file.Feed,
        };
    }
}
