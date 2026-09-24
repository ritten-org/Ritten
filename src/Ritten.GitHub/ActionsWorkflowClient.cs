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
        await file.WriteAllText(Render(workflow), cancellationToken: cancellationToken);
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
