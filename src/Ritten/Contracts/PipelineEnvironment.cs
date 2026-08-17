namespace Ritten.Contracts;

/// <summary>
/// An abstraction for fetching environment variables as the run should see them.
/// </summary>
/// <param name="environment">The filtered environment reads are served from.</param>
public sealed class PipelineEnvironment(Func<string, string?> environment)
{
    /// <summary>
    /// Reads a variable from the filtered environment.
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    public string? Get(string name) => environment(name);
}
