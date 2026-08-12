using Microsoft.Extensions.Logging;
using ILogMessage = NuGet.Common.ILogMessage;
using NuGetILogger = NuGet.Common.ILogger;
using NuGetLogLevel = NuGet.Common.LogLevel;

namespace Wolfe.Hamelin.NuGet;

#pragma warning disable CA2254
/// <summary>
/// Adapts the standard .NET <see cref="ILogger"/> to work with NuGet.
/// </summary>
/// <param name="logger">The logger instance to adapt.</param>
internal class NuGetLoggerAdapter(ILogger logger) : NuGetILogger
{
    /// <inheritdoc/>
    public void LogDebug(string data) => logger.LogDebug(data);

    /// <inheritdoc/>
    public void LogVerbose(string data) => logger.LogTrace(data);

    /// <inheritdoc/>
    public void LogInformation(string data) => logger.LogInformation(data);

    /// <inheritdoc/>
    public void LogMinimal(string data) => logger.LogInformation(data);

    /// <inheritdoc/>
    public void LogWarning(string data) => logger.LogWarning(data);

    /// <inheritdoc/>
    public void LogError(string data) => logger.LogWarning(data);

    /// <inheritdoc/>
    public void LogInformationSummary(string data) => logger.LogInformation(data);

    /// <inheritdoc/>
    public void Log(NuGetLogLevel level, string data) => logger.Log(level switch
    {
        NuGetLogLevel.Debug => LogLevel.Debug,
        NuGetLogLevel.Verbose => LogLevel.Trace,
        NuGetLogLevel.Information => LogLevel.Information,
        NuGetLogLevel.Minimal => LogLevel.Information,
        NuGetLogLevel.Warning => LogLevel.Warning,
        NuGetLogLevel.Error => LogLevel.Error,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    }, data);

    /// <inheritdoc/>
    public Task LogAsync(NuGetLogLevel level, string data) { Log(level, data); return Task.CompletedTask; }

    /// <inheritdoc/>
    public void Log(ILogMessage message) => Log(message.Level, message.Message);

    /// <inheritdoc/>
    public Task LogAsync(ILogMessage message) { Log(message); return Task.CompletedTask; }
}

