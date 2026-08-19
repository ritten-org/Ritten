using Ritten.Contracts;

namespace Ritten.Tests.Support;

[Step("failing", StepKind.Work)]
class FailingStep
{
    // Synchronous on purpose: the failing-step test also covers the sync convention end to end.
    public StepResult Run() => StepResult.Failed("Nope.");
}