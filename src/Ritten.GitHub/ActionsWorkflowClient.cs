using Ritten.Contracts.FileSystem;
using Ritten.Engine;
using YamlDotNet.Core;

namespace Ritten.GitHub;

/// <summary>
/// Reads and writes workflow files where GitHub Actions keeps them.
/// </summary>
internal sealed class ActionsWorkflowClient : IActionsWorkflows
{
    /// <summary>
    /// Where GitHub Actions reads workflows from, by GitHub's convention.
    /// </summary>
    private const string WorkflowDirectory = ".github/workflows";

    /// <inheritdoc />
    public IEnumerable<IFile> Files(IDirectory repository)
    {
        var directory = repository.GetDirectory(WorkflowDirectory);
        return directory.Exists ? [.. directory.GetFiles("*.yml"), .. directory.GetFiles("*.yaml")] : [];
    }

    /// <inheritdoc />
    public IFile File(IDirectory repository, string name) => repository.GetFile($"{WorkflowDirectory}/{name}.yml");

    /// <inheritdoc />
    public async Task<Result<ActionsWorkflow>> Read(IFile file, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(file.OpenRead());
        return Parse(await reader.ReadToEndAsync(cancellationToken));
    }

    /// <inheritdoc />
    public async Task Write(IFile file, ActionsWorkflow workflow, CancellationToken cancellationToken = default)
    {
        file.Directory.Create();

        var stream = file.OpenWrite();
        stream.SetLength(0); // OpenWrite isn't guaranteed to truncate an existing file.
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(Render(workflow).AsMemory(), cancellationToken);
    }

    /// <inheritdoc />
    public Result<ActionsWorkflow> Parse(string yaml)
    {
        try
        {
            return ActionsWorkflow.Parse(yaml);
        }
        catch (YamlException exception)
        {
            return Result.Error($"Could not read the workflow: {exception.Message}", exception);
        }
    }

    /// <inheritdoc />
    public string Render(ActionsWorkflow workflow) => workflow.Text;
}
