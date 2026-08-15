using Microsoft.Extensions.Options;
using Ritten.Contracts.FileSystem;

namespace Ritten.Core.FileSystem;

internal class ProjectFileSystem : IFileSystem
{
    public ProjectFileSystem(RittenProject project, IOptions<PipelineOptions> options)
    {
        ProjectRoot = new PhysicalDirectory(project.Directory);
        Artifacts = ProjectRoot.GetDirectory(options.Value.ArtifactsDirectory);
        Temp = ProjectRoot.GetDirectory(options.Value.TempDirectory);
    }

    /// <inheritdoc />
    public IDirectory ProjectRoot { get; }

    /// <inheritdoc />
    public IDirectory Artifacts { get; }

    /// <inheritdoc />
    public IDirectory Temp { get; }
}
