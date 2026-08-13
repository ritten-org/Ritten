using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Ritten.Runtimes.GitHubActions.Logging;

internal class GitHubActionsConsoleFormatter() : ConsoleFormatter(Constants.FormatterName)
{
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter
    )
    {
        if (logEntry.EventId.Name == Constants.RawCommandEventId.Name)
        {
            var rawMessage = logEntry.Formatter.Invoke(logEntry.State, logEntry.Exception);
            textWriter.WriteLine(rawMessage);
            return;
        }

        switch (logEntry.LogLevel)
        {
            case LogLevel.Critical:
            case LogLevel.Error:
                textWriter.Write("::error::");
                break;
            case LogLevel.Warning:
                textWriter.Write("::warning::");
                break;
            case LogLevel.Information:
                textWriter.Write("Information: ");
                break;
            case LogLevel.Debug:
            case LogLevel.Trace:
                textWriter.Write("::debug::");
                break;
            case LogLevel.None:
            default:
                break;
        }

        var message = logEntry.Formatter.Invoke(logEntry.State, logEntry.Exception);
        message = StringUtils.SanitizeNewLines(message);
        textWriter.Write(message);

        if (logEntry.Exception != null)
        {
            textWriter.Write(StringUtils.UrlEncodedNewLine);
            var exceptionMessage = StringUtils.SanitizeNewLines(logEntry.Exception.ToString());
            textWriter.Write(exceptionMessage);
        }

        textWriter.WriteLine();
    }
}
