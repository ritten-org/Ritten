using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.DotNet;

namespace Ritten.Tests.Support;

/// <summary>
/// Steps declared inline for exercising the engine: probes, failures, and produce/consume pairs.
/// </summary>
public sealed class StepProbe
{
    public List<string> Ran { get; } = [];
}

[Step("probe", StepKind.Work)]
class ProbeStep(StepProbe probe)
{
    public Task<StepResult> Run(CancellationToken cancellationToken)
    {
        probe.Ran.Add(GetType().Name);
        return Task.FromResult(StepResult.Successful);
    }
}

[Step("failing", StepKind.Work)]
class FailingStep
{
    // Synchronous on purpose: the failing-step test also covers the sync convention end to end.
    public StepResult Run() => StepResult.Failed("Nope.");
}

[Step("first", StepKind.Work)]
class FirstStep
{
    public Task<StepResult> Run(CancellationToken cancellationToken = default) =>
        Task.FromResult(StepResult.Successful);
}

[Step("publisher", StepKind.Publish)]
class PublishingStep
{
    public Task<StepResult> Run(CancellationToken cancellationToken) =>
        Task.FromResult(StepResult.Successful);
}

[Step("producer", StepKind.Work)]
class ProjectProducingStep
{
    public Task<StepResult<Project>> Run(CancellationToken cancellationToken) =>
        Task.FromResult<StepResult<Project>>(new Project { Name = "Thing", Version = NuGetVersion.Parse("1.0.0") });
}

[Step("consumer", StepKind.Work)]
class ProjectConsumingStep
{
    public Task<StepResult> Run(Project project, CancellationToken cancellationToken) =>
        Task.FromResult(StepResult.Successful);
}
