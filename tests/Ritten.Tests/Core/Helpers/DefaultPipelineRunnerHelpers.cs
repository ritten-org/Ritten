using Microsoft.Extensions.Logging;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Core.Runner;

namespace Ritten.Tests.Core.Helpers;

internal static class DefaultPipelineRunnerHelpers
{
    public static DefaultPipelineRunner CreateRunner(
        IPipelineStep[]? steps = null,
        IProgressReporter[]? reporters = null,
        Pipeline? pipeline = null,
        ILogger<DefaultPipelineRunner>? logger = null
    )
    {
        logger ??= Substitute.For<ILogger<DefaultPipelineRunner>>();
        pipeline ??= Substitute.For<Pipeline>();

        return new DefaultPipelineRunner(logger, reporters ?? [], steps ?? [], pipeline);
    }
}
