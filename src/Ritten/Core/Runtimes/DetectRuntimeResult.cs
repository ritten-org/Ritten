using Ritten.Contracts;

namespace Ritten.Core.Runtimes;

/// <summary>
/// The outcome of a runtime detection.
/// </summary>
public sealed class DetectRuntimeResult
{
    internal DetectRuntimeResult(Runtime runtime, Func<string, string?> environment)
    {
        Runtime = runtime;
        Raw = environment;
        Environment = name => runtime.Claims.Contains(name) ? null : environment(name);
        Debug = runtime.IsDebug(environment);
    }

    /// <summary>
    /// The runtime detection selected.
    /// </summary>
    public Runtime Runtime { get; }

    /// <summary>
    /// The filtered environment the rest of the run reads: everything the runtime didn't claim.
    /// </summary>
    public Func<string, string?> Environment { get; }

    /// <summary>
    /// The unfiltered environment the runtime was detected in. The runtime reads its own claims
    /// from here; nothing else should.
    /// </summary>
    internal Func<string, string?> Raw { get; }

    /// <summary>
    /// Whether the environment asked for debug logging, as GitHub's "Re-run with debug logging" does.
    /// </summary>
    public bool Debug { get; }

    /// <summary>
    /// Creates the console narrative for the run: the runtime's renderer, at the requested level —
    /// except that a debug request floors the level at Verbose, since re-running with debug logging
    /// is an in-the-moment ask that outranks a --quiet sitting in a workflow file since whenever.
    /// </summary>
    /// <param name="requested">The lowest level of message the command line asked to print.</param>
    public IPipelineConsole CreateConsole(PipelineLogLevel requested) =>
        Runtime.CreateConsole(Debug ? PipelineLogLevel.Verbose : requested);
}
