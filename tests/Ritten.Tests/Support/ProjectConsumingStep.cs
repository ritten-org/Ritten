using Ritten.Contracts;
using Ritten.DotNet;

namespace Ritten.Tests.Support;

[Step("consumer", StepKind.Work)]
class ProjectConsumingStep
{
    public Task<StepResult> Run(Project project, CancellationToken cancellationToken) =>
        Task.FromResult(StepResult.Successful);
}