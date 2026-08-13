namespace Ritten.Commands;

/// <summary>
/// Represents a command that can be run by <see cref="ICommandRunner"/>.
/// </summary>
public record Command
{
    /// <summary>
    /// The path to the executable to run.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Any arguments to pass to the command.
    /// </summary>
    public string[] Arguments { get; init; } = [];

    /// <summary>
    /// Any environment variables to pass to the command.
    /// </summary>
    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// The text to pipe to standard input.
    /// </summary>
    public string? StandardInput { get; init; }

    /// <summary>
    /// The directory to run the command in.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Indicates whether the command's arguments contain sensitive values.
    /// </summary>
    public bool ArgumentsRedacted { get; init; }

    /// <summary>
    /// Indicates whether the command's output contains sensitive values.
    /// </summary>
    public bool OutputRedacted { get; init; }

    /// <summary>
    /// Indicates whether the runner throws a <see cref="CommandFailedException"/> when the command exits non-zero.
    /// </summary>
    public bool ThrowsOnError { get; init; }

    /// <summary>
    /// Creates a new command for the executable at the given path.
    /// </summary>
    public static Command Create(string path) => new() { Path = path };

    /// <summary>
    /// Returns a copy of the command with its arguments replaced by the given values.
    /// </summary>
    public Command WithArguments(params string[] arguments) => this with { Arguments = arguments };

    /// <summary>
    /// Returns a copy of the command with the given values appended to its arguments.
    /// </summary>
    public Command AndArguments(params string[] arguments) => this with { Arguments = [.. Arguments, .. arguments] };

    /// <summary>
    /// Returns a copy of the command with its environment variables replaced by the given values.
    /// </summary>
    public Command WithEnvironmentVariables(IReadOnlyDictionary<string, string> envVars) => this with { EnvironmentVariables = envVars };

    /// <summary>
    /// Returns a copy of the command that pipes the given text to standard input.
    /// </summary>
    public Command WithInput(string input) => this with { StandardInput = input };

    /// <summary>
    /// Returns a copy of the command that runs in the given directory.
    /// </summary>
    public Command InDirectory(string path) => this with { WorkingDirectory = path };

    /// <summary>
    /// Returns a copy of the command with its arguments redacted from the logs.
    /// </summary>
    public Command RedactArguments() => this with { ArgumentsRedacted = true };

    /// <summary>
    /// Returns a copy of the command with its output redacted from the logs.
    /// </summary>
    public Command RedactOutput() => this with { OutputRedacted = true };

    /// <summary>
    /// Returns a copy of the command with both its arguments and output redacted from the logs.
    /// </summary>
    public Command Sensitive() => this with { ArgumentsRedacted = true, OutputRedacted = true };

    /// <summary>
    /// Returns a copy of the command that throws a <see cref="CommandFailedException"/> if it exits non-zero.
    /// </summary>
    public Command ThrowOnError() => this with { ThrowsOnError = true };

}
