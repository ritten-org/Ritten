using Ritten.Contracts;

namespace Ritten.Tests.Support;

[Step("publisher", StepKind.Publish)]
class PublishingStep
{
    public Task<StepResult> Run(CancellationToken cancellationToken) =>
        Task.FromResult(StepResult.Successful);
}