namespace Ritten.Commands;

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
    /// The last few lines the command wrote stderr.
    /// </summary>
    /// <param name="lines">The maximum number of lines to keep.</param>
    public IReadOnlyList<string> ErrorTail(int lines = 10)
    {
        var tail = Tail(StandardError, lines);
        return tail.Count > 0 ? tail : Tail(StandardOutput, lines);
    }

    private static List<string> Tail(string output, int lines) =>
    [
        .. output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .TakeLast(lines)
    ];
}
