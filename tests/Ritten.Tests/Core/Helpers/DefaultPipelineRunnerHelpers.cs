using Microsoft.Extensions.Logging;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Core.Runner;
using Ritten.Core.Steps;

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

        var stepProvider = Substitute.For<IPipelineStepProvider>();
        stepProvider.GetSteps().Returns(steps ?? []);

        return new DefaultPipelineRunner(logger, reporters ?? [], stepProvider, pipeline);
    }
}
