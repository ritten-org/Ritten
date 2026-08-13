using Ritten.Contracts;
using ILogMessage = NuGet.Common.ILogMessage;
using NuGetILogger = NuGet.Common.ILogger;
using NuGetLogLevel = NuGet.Common.LogLevel;

namespace Ritten.NuGet;

/// <summary>
/// Adapts <see cref="IPipelineLog"/> to NuGet's <see cref="NuGetILogger"/>.
/// </summary>
/// <param name="log">The pipeline log to write to.</param>
internal class NuGetLoggerAdapter(IPipelineLog log) : NuGetILogger
{
    /// <inheritdoc/>
    public void LogDebug(string data) => log.Verbose(data);

    /// <inheritdoc/>
    public void LogVerbose(string data) => log.Verbose(data);

    /// <inheritdoc/>
    public void LogInformation(string data) => log.Verbose(data);

    /// <inheritdoc/>
    public void LogMinimal(string data) => log.Verbose(data);

    /// <inheritdoc/>
    public void LogWarning(string data) => log.Verbose(data);

    /// <inheritdoc/>
    public void LogError(string data) => log.Verbose(data);

    /// <inheritdoc/>
    public void LogInformationSummary(string data) => log.Verbose(data);

    /// <inheritdoc/>
    public void Log(NuGetLogLevel level, string data) => log.Verbose(data);

    /// <inheritdoc/>
    public Task LogAsync(NuGetLogLevel level, string data) { Log(level, data); return Task.CompletedTask; }

    /// <inheritdoc/>
    public void Log(ILogMessage message) => Log(message.Level, message.Message);

    /// <inheritdoc/>
    public Task LogAsync(ILogMessage message) { Log(message); return Task.CompletedTask; }
}
