namespace Ritten.Docker;

/// <summary>
/// One container run to completion. Environment values reach the container through the daemon
/// rather than the argument list, so a secret among them never appears in a process table.
/// </summary>
/// <param name="Image">The image to run.</param>
/// <param name="Arguments">The command line inside the container; the image's entrypoint receives it.</param>
public sealed record ContainerRun(string Image, IReadOnlyList<string> Arguments)
{
    /// <summary>
    /// Host directories bound into the container.
    /// </summary>
    public IReadOnlyList<BindMount> Mounts { get; init; } = [];

    /// <summary>
    /// Variables set inside the container.
    /// </summary>
    public IReadOnlyDictionary<string, string> Environment { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// A network to join, when the container must reach others by name.
    /// </summary>
    public string? Network { get; init; }
}
