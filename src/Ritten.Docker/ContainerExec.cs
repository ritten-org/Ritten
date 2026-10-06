namespace Ritten.Docker;

/// <summary>
/// A command run in a container that is already running.
/// </summary>
/// <param name="Container">The container's name or id.</param>
/// <param name="Arguments">The command line to run inside it.</param>
public sealed record ContainerExec(string Container, IReadOnlyList<string> Arguments)
{
    /// <summary>
    /// The user to run it as, when not the container's.
    /// </summary>
    public string? User { get; init; }

    /// <summary>
    /// The standard input to pipe to the container.
    /// </summary>
    public string? Input { get; init; }

    /// <summary>
    /// Determines whether the call will be executed for real during a dry run.
    /// </summary>
    public bool IsReadOnly { get; init; }
}
