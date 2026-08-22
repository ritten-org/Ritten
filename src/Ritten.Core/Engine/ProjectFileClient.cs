using System.Text.Json;
using System.Text.Json.Nodes;
using Ritten.Contracts.FileSystem;

namespace Ritten.Engine;

/// <summary>
/// Reads and writes project files tolerantly.
/// </summary>
internal sealed class ProjectFileClient : IProjectFiles
{
    private static readonly JsonNodeOptions NodeOptions = new() { PropertyNameCaseInsensitive = false };

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <inheritdoc />
    public async Task<Result<ProjectFile>> Read(IFile file, CancellationToken cancellationToken = default)
    {
        if (!file.Exists)
        {
            return ProjectFile.Empty;
        }

        using var reader = new StreamReader(file.OpenRead());
        var document = Parse(await reader.ReadToEndAsync(cancellationToken));
        return document.IsError
            ? new Result<ProjectFile>([Result.Error($"Could not read '{file.Name}': {document.Errors.First().Message}")])
            : document;
    }

    /// <inheritdoc />
    public async Task Write(IFile file, ProjectFile document, CancellationToken cancellationToken = default)
    {
        var stream = file.OpenWrite();
        stream.SetLength(0); // OpenWrite isn't guaranteed to truncate an existing file.
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(Render(document).AsMemory(), cancellationToken);
    }

    /// <inheritdoc />
    public Result<ProjectFile> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ProjectFile.Empty;
        }

        try
        {
            // Anything but an object — a list, a number — is a project file nobody could have
            // meant, so it's refused rather than quietly replaced.
            return JsonNode.Parse(json, NodeOptions, DocumentOptions) is JsonObject root
                ? new ProjectFile(root)
                : Result.Error("it isn't an object.");
        }
        catch (JsonException exception)
        {
            return Result.Error(exception.Message, exception);
        }
    }

    /// <inheritdoc />
    public string Render(ProjectFile document) => document.ToString();
}
