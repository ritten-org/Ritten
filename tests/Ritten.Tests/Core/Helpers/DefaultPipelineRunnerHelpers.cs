using Ritten.Contracts;
using Ritten.Core.Runner;

namespace Ritten.Tests.Core.Helpers;

internal static class DefaultPipelineRunnerHelpers
{
    public static DefaultPipelineRunner CreateRunner(
        IPipelineStep[]? steps = null,
        IProgressReporter[]? reporters = null,
        PipelineJob? job = null,
        IPipelineLog? log = null
    )
    {
        log ??= Substitute.For<IPipelineLog>();
        job ??= new PipelineJob("Test", "verify");

        return new DefaultPipelineRunner(log, reporters ?? [], steps ?? [], job);
    }
}
