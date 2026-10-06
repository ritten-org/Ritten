using System.Collections.Concurrent;
using System.Diagnostics;
using Ritten.Reporting;

namespace Ritten.Tests.Support;

/// <summary>
/// Listens to Ritten's spans as a tracer provider would. A listener hears every run in the process, and tests run in
/// parallel, so a test reads only the trace it started.
/// </summary>
internal sealed class TraceCollector : IDisposable
{
    private readonly ConcurrentQueue<Activity> _stopped = new();
    private readonly ActivityListener _listener;

    public TraceCollector()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == RittenTelemetry.SourceName,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _stopped.Enqueue
        };
        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>
    /// The spans of one trace that have finished, in the order they finished.
    /// </summary>
    public IReadOnlyList<Activity> Trace(ActivityTraceId traceId) => [.. _stopped.Where(activity => activity.TraceId == traceId)];

    public void Dispose() => _listener.Dispose();
}
