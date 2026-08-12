using Microsoft.Extensions.Logging;
using Wolfe.Hamelin.NuGet;

namespace Wolfe.Hamelin.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ILogger"/>.
/// </summary>
public static class LoggerExtensions
{
    extension(ILogger logger)
    {
        /// <summary>
        /// Adapts the logger to be compatible with NuGet.
        /// </summary>
        /// <returns>The compatible logger.</returns>
        public global::NuGet.Common.ILogger ForNuGet() => new NuGetLoggerAdapter(logger);
    }
}
