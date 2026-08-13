using Microsoft.Extensions.Logging;
using Ritten.Core.Runner;

namespace Ritten.Tests.Core.Helpers;

internal static class DefaultPipelineStepRunnerHelpers
{
    public static DefaultPipelineStepRunner CreateRunner(
        ILogger<DefaultPipelineStepRunner>? logger = null
    )
    {
        logger ??= Substitute.For<ILogger<DefaultPipelineStepRunner>>();
        return new DefaultPipelineStepRunner(logger);
    }
}
