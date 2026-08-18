using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.Engine.Runtimes;

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
    /// The filtered environment the rest of the run reads.
    /// </summary>
    public Func<string, string?> Environment { get; }

    /// <summary>
    /// The unfiltered environment the runtime was detected in.
    /// </summary>
    internal Func<string, string?> Raw { get; }

    /// <summary>
    /// Whether the environment asked for debug logging.
    /// </summary>
    public bool Debug { get; }

    /// <summary>
    /// Creates the console narrative for the run.
    /// </summary>
    /// <param name="requested">The lowest level of message the command line asked to print.</param>
    public IWorkflowConsole CreateConsole(WorkflowLogLevel requested) =>
        Runtime.CreateConsole(Debug ? WorkflowLogLevel.Verbose : requested);
}
