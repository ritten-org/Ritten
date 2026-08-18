using Ritten.Contracts;

namespace Ritten.NuGet;

/// <summary>
/// Provides extension methods for <see cref="IWorkflowLog"/>.
/// </summary>
public static class LoggerExtensions
{
    extension(IWorkflowLog log)
    {
        /// <summary>
        /// Adapts the workflow log to be compatible with NuGet.
        /// </summary>
        /// <returns>The compatible logger.</returns>
        public global::NuGet.Common.ILogger ForNuGet() => new NuGetLoggerAdapter(log);
    }
}
