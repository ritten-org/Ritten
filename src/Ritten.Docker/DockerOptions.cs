namespace Ritten.Docker;

/// <summary>
/// What the docker steps work on.
/// </summary>
public sealed class DockerOptions
{
    /// <summary>
    /// The images this component builds from its own source.
    /// </summary>
    public IReadOnlyList<DockerImage> Images { get; set; } = [];
}

/// <summary>
/// An image a component builds.
/// </summary>
/// <param name="Tag">The tag the compose file refers to.</param>
/// <param name="Context">The build context, relative to the component.</param>
public sealed record DockerImage(string Tag, string Context);
