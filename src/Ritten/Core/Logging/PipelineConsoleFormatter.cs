using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Ritten.Core.Logging;

internal class PipelineConsoleFormatter : ConsoleFormatter, IDisposable
{
    internal const string FormatterName = "RittenConsole";

    private readonly IDisposable? _optionsReloadToken;
    private readonly TimeProvider _time;

    public PipelineConsoleFormatter(IOptionsMonitor<PipelineConsoleFormatterOptions> options, TimeProvider timeProvider) : base(FormatterName)
    {
        ReloadLoggerOptions(options.CurrentValue);
        _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        _time = timeProvider;
    }

    [MemberNotNull(nameof(FormatterOptions))]
    private void ReloadLoggerOptions(PipelineConsoleFormatterOptions options)
    {
        FormatterOptions = options;
    }

    public void Dispose()
    {
        _optionsReloadToken?.Dispose();
    }

    internal PipelineConsoleFormatterOptions FormatterOptions { get; set; }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);

        if (logEntry.Exception is null && string.IsNullOrEmpty(message))
        {
            return;
        }

        var logTime = GetCurrentDateTime();

        var category = GetStepName(scopeProvider);

        WriteMessage(textWriter, message, logTime, logEntry.LogLevel, category);

        if (logEntry.Exception is not null)
        {
            WriteMessage(textWriter, logEntry.Exception.ToString(), logTime, logEntry.LogLevel, category);
        }
    }

    private string? GetStepName(IExternalScopeProvider? scopeProvider)
    {
        if (scopeProvider is null || !FormatterOptions.IncludeScopes || !FormatterOptions.IncludeStepNames)
        {
            return null;
        }

        string? stepName = null;

        scopeProvider.ForEachScope((scope, state) =>
        {
            if (scope is not LogAttributes logAttributes)
            {
                return;
            }

            stepName = logAttributes.StepName;
        }, stepName);

        return stepName;
    }

    private void WriteMessage(TextWriter textWriter, string? message, DateTimeOffset logTime, LogLevel logLevel, string? stepName)
    {
        if (message is null)
        {
            return;
        }

        var messageLines = message.Split(Environment.NewLine);
        foreach (var messageLine in messageLines)
        {
            WriteMessageLine(textWriter, messageLine, logTime, logLevel, stepName);
        }
    }

    private void WriteMessageLine(TextWriter textWriter, string message, DateTimeOffset logTime, LogLevel logLevel, string? stepName)
    {
        var logLevelColors = GetLogLevelConsoleColors(logLevel);
        var logLevelString = GetLogLevelString(logLevel);

        string? timestamp = null;
        var timestampFormat = FormatterOptions.TimestampFormat;
        if (timestampFormat != null)
        {
            timestamp = logTime.ToString(timestampFormat);
            textWriter.Write(' ');
        }

        if (!string.IsNullOrEmpty(timestamp))
        {
            textWriter.Write(timestamp);
            textWriter.Write(' ');
        }

        if (!string.IsNullOrEmpty(logLevelString))
        {
            textWriter.WriteColoredMessage(logLevelString, logLevelColors.Background, logLevelColors.Foreground);
            textWriter.Write(' ');
        }

        if (stepName is not null)
        {
            textWriter.Write(stepName);
            textWriter.Write(": ");
        }

        textWriter.WriteLine(message);
    }

    private DateTimeOffset GetCurrentDateTime()
    {
        if (FormatterOptions.TimestampFormat is null)
        {
            return DateTimeOffset.MinValue;
        }

        return FormatterOptions.UseUtcTimestamp ? _time.GetUtcNow() : _time.GetLocalNow();
    }

    private static string GetLogLevelString(LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.None => string.Empty,
            LogLevel.Trace => LogMessageLevels.Trace,
            LogLevel.Debug => LogMessageLevels.Debug,
            LogLevel.Information => LogMessageLevels.Information,
            LogLevel.Warning => LogMessageLevels.Warning,
            LogLevel.Error => LogMessageLevels.Error,
            LogLevel.Critical => LogMessageLevels.Critical,
            _ => $"[LEVEL {logLevel}]"
        };

    private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
    {
        if (FormatterOptions.ColorBehavior == LoggerColorBehavior.Disabled)
        {
            return new ConsoleColors(null, null);
        }

        return logLevel switch
        {
            LogLevel.Trace => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
            LogLevel.Debug => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
            LogLevel.Information => new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black),
            LogLevel.Warning => new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black),
            LogLevel.Error => new ConsoleColors(ConsoleColor.Red, ConsoleColor.Black),
            LogLevel.Critical => new ConsoleColors(ConsoleColor.White, ConsoleColor.DarkRed),
            _ => new ConsoleColors(null, null)
        };
    }

    private readonly struct ConsoleColors(ConsoleColor? foreground, ConsoleColor? background)
    {
        public ConsoleColor? Foreground { get; } = foreground;

        public ConsoleColor? Background { get; } = background;
    }
}
