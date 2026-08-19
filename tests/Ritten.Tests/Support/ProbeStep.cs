using Ritten.Contracts;

namespace Ritten.Tests.Support;

[Step("probe", StepKind.Work)]
class ProbeStep(StepProbe probe)
{
    public Task<StepResult> Run(CancellationToken cancellationToken)
    {
        probe.Ran.Add(GetType().Name);
        return Task.FromResult(StepResult.Successful);
    }
}
