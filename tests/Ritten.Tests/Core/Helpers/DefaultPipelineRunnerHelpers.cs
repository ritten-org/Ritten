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
        IPipelineLog? log = null
    )
    {
        log ??= Substitute.For<IPipelineLog>();
        pipeline ??= Substitute.For<Pipeline>();

        return new DefaultPipelineRunner(log, reporters ?? [], steps ?? [], pipeline);
    }
}
