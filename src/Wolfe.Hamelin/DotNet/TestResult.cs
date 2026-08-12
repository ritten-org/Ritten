namespace Wolfe.Hamelin.DotNet;

/// <summary>
/// The outcome of a <see cref="IDotNet.Test"/> invocation.
/// </summary>
public record TestResult : TestRun
{
    /// <summary>
    /// True if the test command succeeded, otherwise false.
    /// </summary>
    public required bool Succeeded { get; init; }
}
