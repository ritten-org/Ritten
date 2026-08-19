using Ritten.Contracts;

namespace Ritten.Tests.Support;

[Step("first", StepKind.Work)]
class FirstStep
{
    public Task<StepResult> Run(CancellationToken cancellationToken = default) =>
        Task.FromResult(StepResult.Successful);
}