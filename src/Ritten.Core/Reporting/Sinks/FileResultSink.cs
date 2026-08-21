using Ritten.Contracts.FileSystem;

namespace Ritten.Reporting.Sinks;

/// <summary>
/// Writes the report to a file in the artifacts directory.
/// </summary>
/// <param name="log">The workflow log.</param>
/// <param name="renderer">The renderer that turns the report into markdown.</param>
/// <param name="fileSystem">The file system.</param>
internal class FileResultSink(IWorkflowLog log, MarkdownReportRenderer renderer, IFileSystem fileSystem) : IWorkflowResultSink
{
    /// <summary>
    /// The name the report is written under.
    /// </summary>
    internal const string FileName = "report.md";

    /// <inheritdoc />
    public async Task Publish(WorkflowReport report, CancellationToken cancellationToken = default)
    {
        fileSystem.Artifacts.Create();
        var file = fileSystem.Artifacts.GetFile(FileName);

        var stream = file.OpenWrite();
        stream.SetLength(0); // OpenWrite isn't guaranteed to truncate an existing file.
        await using (var writer = new StreamWriter(stream))
        {
            await writer.WriteAsync(renderer.Render(report).AsMemory(), cancellationToken);
        }

        log.Detail($"Wrote the report to {fileSystem.Artifacts.Name}/{FileName}.");
    }
}
