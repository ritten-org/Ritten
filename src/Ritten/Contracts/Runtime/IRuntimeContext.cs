namespace Ritten.Contracts.Runtime;

/// <summary>
/// Provides information about the runtime environment in which the pipeline is executing.
/// </summary>
public interface IRuntimeContext
{
    /// <summary>
    /// Gets a value indicating whether the pipeline is running inside a CI environment.
    /// </summary>
    bool IsCI { get; }
}
