using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Core.Runner;

namespace Ritten.Tests.Core.Helpers;

internal static class PipelineStepRunnerHelpers
{
    public static IPipelineStepRunner CreateMock()
    {
        var stepRunner = Substitute.For<IPipelineStepRunner>();
        stepRunner
            .RunStep(Arg.Any<AsyncServiceScope>(), Arg.Any<IPipelineStep>(), Arg.Any<CancellationToken>())
            .Returns(new StepExecutionSummary { StepName = "", Result = PipelineStepResult.Successful });
        return stepRunner;
    }
}
