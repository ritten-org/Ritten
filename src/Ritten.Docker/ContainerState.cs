namespace Ritten.Docker;

/// <summary>
/// What the daemon knows of a container at one moment.
/// </summary>
/// <param name="Image">The image reference the container was created from, as it was written.</param>
/// <param name="Running">Whether the container is running.</param>
public sealed record ContainerState(string Image, bool Running);
