using Ritten.Contracts.FileSystem;

namespace Ritten.Docker;

/// <summary>
/// Exposes functionality for the Docker daemon the workflow can reach: images, one-shot
/// containers, and compose stacks.
/// </summary>
public interface IDocker
{
    /// <summary>
    /// Builds the Dockerfile in the given directory into an image with the given tag.
    /// </summary>
    /// <param name="context">The build context, holding the Dockerfile.</param>
    /// <param name="tag">The tag to give the image.</param>
    /// <param name="platform">A target platform such as <c>linux/amd64</c>, when it must differ from the daemon's.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task Build(IDirectory context, string tag, string? platform = null, CancellationToken ct = default);

    /// <summary>
    /// Gives an existing image another name.
    /// </summary>
    Task Tag(string source, string target, CancellationToken ct = default);

    /// <summary>
    /// Pushes the given image to its registry.
    /// </summary>
    Task Push(string image, CancellationToken ct = default);

    /// <summary>
    /// Logs the daemon in to a registry. The password reaches docker on its standard input,
    /// never its command line.
    /// </summary>
    Task Login(string registry, string username, string password, CancellationToken ct = default);

    /// <summary>
    /// Runs a container to completion and removes it.
    /// </summary>
    Task Run(ContainerRun run, CancellationToken ct = default);

    /// <summary>
    /// Converges the compose stack in the given project directory: creates or recreates what
    /// the file declares, removes what it no longer does.
    /// </summary>
    /// <param name="project">The directory holding the compose file.</param>
    /// <param name="environment">Values the compose file interpolates, secrets included; they reach compose through its environment.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task ComposeUp(IDirectory project, IReadOnlyDictionary<string, string>? environment = null, CancellationToken ct = default);

    /// <summary>
    /// Stops and removes the compose stack in the given project directory. Volumes stay.
    /// </summary>
    Task ComposeDown(IDirectory project, CancellationToken ct = default);
}
