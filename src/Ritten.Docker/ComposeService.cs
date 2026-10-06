namespace Ritten.Docker;

/// <summary>
/// One service of a <see cref="ComposeProject"/>.
/// </summary>
/// <param name="Name">The service's name in the compose file.</param>
/// <param name="Labels">Its Docker labels.</param>
/// <param name="Environment">Its environment; a variable declared without a value is null.</param>
/// <param name="Ports">The ports it publishes.</param>
/// <param name="NetworkMode">Whose network it runs in, when not its project's own: <c>service:web</c>, <c>host</c>.</param>
public sealed record ComposeService(
    string Name,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyDictionary<string, string?> Environment,
    IReadOnlyList<ComposePort> Ports,
    string? NetworkMode = null)
{
    /// <summary>
    /// The name it gives its container, when it gives one (<c>container_name</c>).
    /// </summary>
    public string? ContainerName { get; init; }
}
