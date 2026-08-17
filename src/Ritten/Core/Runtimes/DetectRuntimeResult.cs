namespace Ritten.Core.Runtimes;

/// <summary>
/// The outcome of a runtime detection.
/// </summary>
public sealed class DetectRuntimeResult
{
    internal DetectRuntimeResult(Runtime runtime, Func<string, string?> environment)
    {
        Runtime = runtime;
        Environment = environment;
    }

    /// <summary>
    /// The runtime detection selected.
    /// </summary>
    public Runtime Runtime { get; }

    /// <summary>
    /// The filtered environment the rest of the run reads.
    /// </summary>
    public Func<string, string?> Environment { get; }
}
