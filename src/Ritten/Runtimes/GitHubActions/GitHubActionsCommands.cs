using Ritten.Contracts.Runtime;
using Ritten.Runtimes.GitHubActions.Logging;
using Microsoft.Extensions.Logging;

namespace Ritten.Runtimes.GitHubActions;

/// <remarks>
/// Based on https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-commands
/// </remarks>
internal class GitHubActionsCommands(ILogger<GitHubActionsCommands> logger) : IRuntimeCommands
{
    public void LogDebug(string message) => WriteCommand("debug", message, null);

    public void LogNotice(
        string message,
        string? title = null,
        string? file = null,
        int? startLine = null,
        int? endLine = null,
        int? startColumn = null,
        int? endColumn = null
    ) => WriteFileCommand("notice", message, title, file, startLine, endLine, startColumn, endColumn);

    public void LogWarning(
        string message,
        string? title = null,
        string? file = null,
        int? startLine = null,
        int? endLine = null,
        int? startColumn = null,
        int? endColumn = null
    ) => WriteFileCommand("warning", message, title, file, startLine, endLine, startColumn, endColumn);

    public void LogError(
        string message,
        string? title = null,
        string? file = null,
        int? startLine = null,
        int? endLine = null,
        int? startColumn = null,
        int? endColumn = null
    ) => WriteFileCommand("error", message, title, file, startLine, endLine, startColumn, endColumn);

    public void BeginGroup(string title)
    {
        WriteCommand("group", title, null);
    }

    public void EndGroup()
    {
        WriteCommand("endgroup", "", null);
    }

    public IDisposable WithGroup(string title)
    {
        BeginGroup(title);
        return new DisposableGroup(this);
    }

    public async Task AppendJobSummary(string summary, CancellationToken cancellationToken = default)
    {
        const string envVarName = "GITHUB_STEP_SUMMARY";
        var path = Environment.GetEnvironmentVariable(envVarName);
        if (path == null)
        {
            throw new InvalidOperationException($"Environment variable '{envVarName}' is not set. " +
                                                $"Ensure that you are running this in a GitHub Actions environment.");
        }

        await File.AppendAllTextAsync(path, summary, cancellationToken);
    }

    private void WriteFileCommand(
        string command,
        string message,
        string? title = null,
        string? file = null,
        int? startLine = null,
        int? endLine = null,
        int? startColumn = null,
        int? endColumn = null
    )
    {
        var args = new Dictionary<string, string?>
        {
            { "title", title },
            { "file", file },
            { "line", startLine?.ToString() },
            { "endLine", endLine?.ToString() },
            { "col", startColumn?.ToString() },
            { "endColumn", endColumn?.ToString() }
        };
        WriteCommand(command, message, args);
    }

    private void WriteCommand(string command, string message, Dictionary<string, string?>? args)
    {
        var argString = "";
        if (args != null)
        {
            var existingArgs = args
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                .Select(kvp => $"{kvp.Key}={kvp.Value}")
                .ToArray();
            if (existingArgs.Length > 0)
            {
                argString = " " + string.Join(",", existingArgs);
            }
        }

        message = StringUtils.SanitizeNewLines(message);
        var commandText = $"::{command}{argString}::{message}";

        logger.LogInformation(Constants.RawCommandEventId, "{Command}", commandText);
    }

    private class DisposableGroup(IRuntimeCommands commands) : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            commands.EndGroup();
        }
    }
}
