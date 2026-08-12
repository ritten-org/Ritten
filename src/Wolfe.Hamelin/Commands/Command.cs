using Microsoft.Extensions.Logging;

namespace Wolfe.Hamelin.Commands;

/// <summary>
/// Represents a command that can be run by <see cref="CommandRunner"/>
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
    /// The stream to pipe to standard in.
    /// </summary>
    public string? StandardInput { get; init; }

    /// <summary>
    /// Indicates whether the command contains sensitive output, so it can be redacted from the logs.
    /// </summary>
    public bool IsSensitive { get; init; }

    /// <summary>
    /// Set the level at which stderr is logged to the console.
    /// </summary>
    public LogLevel StandardErrorLogLevel { get; init; } = LogLevel.Error;

    /// <summary>
    /// Creates a new command with the given path.
    /// </summary>
    public static Command Run(string path) => new() { Path = path };

    /// <summary>
    /// Runs the command with the given arguments.
    /// </summary>
    public Command WithArguments(params string[] arguments) => this with { Arguments = arguments };

    /// <summary>
    /// Runs the command with the given environment variables.
    /// </summary>
    public Command WithEnvironmentVariables(IReadOnlyDictionary<string, string> envVars) => this with { EnvironmentVariables = envVars };

    /// <summary>
    /// Marks the command as sensitive.
    /// </summary>
    public Command Sensitive() => this with { IsSensitive = true };

    /// <summary>
    /// Pipes the given input to the command.
    /// </summary>
    public Command WithInput(string input) => this with { StandardInput = input };

    /// <summary>
    /// Reports standard error at the given log level (defaults to error).
    /// </summary>
    public Command ReportStandardError(LogLevel logLevel) => this with { StandardErrorLogLevel = logLevel };
}
