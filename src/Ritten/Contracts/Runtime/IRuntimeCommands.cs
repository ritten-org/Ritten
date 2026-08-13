namespace Ritten.Contracts.Runtime;

/// <summary>
/// Provides CI-agnostic commands for interacting with the runtime environment.
/// </summary>
public interface IRuntimeCommands
{
    /// <summary>
    /// Logs a debug message visible only when debug logging is enabled in the runtime.
    /// </summary>
    /// <param name="message">The debug message to log.</param>
    void LogDebug(string message);

    /// <summary>
    /// Logs a notice-level annotation.
    /// </summary>
    /// <param name="message">The notice message to log.</param>
    /// <param name="title">An optional title for the annotation.</param>
    /// <param name="file">An optional file path associated with the annotation.</param>
    /// <param name="startLine">An optional start line number in the file.</param>
    /// <param name="endLine">An optional end line number in the file.</param>
    /// <param name="startColumn">An optional start column number in the file.</param>
    /// <param name="endColumn">An optional end column number in the file.</param>
    void LogNotice(
        string message,
        string? title = null,
        string? file = null,
        int? startLine = null,
        int? endLine = null,
        int? startColumn = null,
        int? endColumn = null
    );

    /// <summary>
    /// Logs a warning-level annotation.
    /// </summary>
    /// <param name="message">The warning message to log.</param>
    /// <param name="title">An optional title for the annotation.</param>
    /// <param name="file">An optional file path associated with the annotation.</param>
    /// <param name="startLine">An optional start line number in the file.</param>
    /// <param name="endLine">An optional end line number in the file.</param>
    /// <param name="startColumn">An optional start column number in the file.</param>
    /// <param name="endColumn">An optional end column number in the file.</param>
    void LogWarning(
        string message,
        string? title = null,
        string? file = null,
        int? startLine = null,
        int? endLine = null,
        int? startColumn = null,
        int? endColumn = null
    );

    /// <summary>
    /// Logs an error-level annotation.
    /// </summary>
    /// <param name="message">The error message to log.</param>
    /// <param name="title">An optional title for the annotation.</param>
    /// <param name="file">An optional file path associated with the annotation.</param>
    /// <param name="startLine">An optional start line number in the file.</param>
    /// <param name="endLine">An optional end line number in the file.</param>
    /// <param name="startColumn">An optional start column number in the file.</param>
    /// <param name="endColumn">An optional end column number in the file.</param>
    void LogError(
        string message,
        string? title = null,
        string? file = null,
        int? startLine = null,
        int? endLine = null,
        int? startColumn = null,
        int? endColumn = null
    );

    /// <summary>
    /// Starts an expandable group in the runtime log output.
    /// </summary>
    /// <param name="title">The title of the group.</param>
    void BeginGroup(string title);

    /// <summary>
    /// Completes the current expandable group in the runtime log output.
    /// </summary>
    void EndGroup();

    /// <summary>
    /// Starts an expandable group that completes when the returned object is disposed.
    /// </summary>
    /// <param name="title">The title of the group.</param>
    /// <returns>A disposable object that will complete the group.</returns>
    IDisposable WithGroup(string title);

    /// <summary>
    /// Appends content to the job summary for the current run.
    /// </summary>
    /// <param name="summary">The summary text to append. Markdown is supported.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    Task AppendJobSummary(string summary, CancellationToken cancellationToken = default);
}
