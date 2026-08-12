namespace Wolfe.Hamelin.Commands;

/// <summary>
/// Represents the result of running a command.
/// </summary>
/// <param name="ExitCode">The exit code of the process.</param>
/// <param name="StandardOutput">The contents of standard out pipe.</param>
/// <param name="StandardError">The contents of the standard error pipe.</param>
public record CommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    /// <summary>
    /// True if the command completed successfully, otherwise false.
    /// </summary>
    public bool IsSuccess => ExitCode == 0;

    /// <summary>
    /// True if the command exited non-zero, otherwise false.
    /// </summary>
    public bool IsError => ExitCode != 0;

    /// <summary>
    /// Throws an exception if <see cref="IsError"/> is true.
    /// </summary>
    public void ThrowOnError()
    {
        // TODO: implement.
    }
}
