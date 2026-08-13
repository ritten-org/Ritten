using Ritten.Contracts;
using Ritten.NuGet;

namespace Ritten.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IPipelineLog"/>.
/// </summary>
public static class LoggerExtensions
{
    extension(IPipelineLog log)
    {
        /// <summary>
        /// Adapts the pipeline log to be compatible with NuGet.
        /// </summary>
        /// <returns>The compatible logger.</returns>
        public global::NuGet.Common.ILogger ForNuGet() => new NuGetLoggerAdapter(log);
    }
}
