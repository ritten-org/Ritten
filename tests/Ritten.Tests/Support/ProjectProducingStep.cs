using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.DotNet;

namespace Ritten.Tests.Support;

[Step("producer", StepKind.Work)]
class ProjectProducingStep
{
    public Task<StepResult<Project>> Run(CancellationToken cancellationToken) =>
        Task.FromResult<StepResult<Project>>(new Project { Name = "Thing", Version = NuGetVersion.Parse("1.0.0") });
}
