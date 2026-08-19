namespace Ritten.Tests.Support;

/// <summary>
/// Steps declared inline for exercising the engine: probes, failures, and produce/consume pairs.
/// </summary>
public sealed class StepProbe
{
    public List<string> Ran { get; } = [];
}