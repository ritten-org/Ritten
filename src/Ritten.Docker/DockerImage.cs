namespace Ritten.Docker;

/// <summary>
/// An image a component builds.
/// </summary>
/// <param name="Tag">The tag the compose file refers to.</param>
/// <param name="Context">The build context, relative to the component.</param>
public sealed record DockerImage(string Tag, string Context);