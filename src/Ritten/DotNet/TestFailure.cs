namespace Ritten.DotNet;

/// <summary>
/// A single failed test from a <see cref="TestRun"/>.
/// </summary>
/// <param name="TestName">The name of the failed test.</param>
/// <param name="Message">The failure message, or an empty string if the TRX file didn't include one.</param>
public record TestFailure(string TestName, string Message);
