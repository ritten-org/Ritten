using System.ComponentModel;
using Hamelin;
using Wolfe.Hamelin.DotNet;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Restore Dependencies")]
public class Restore(IDotNet dotnet) : IPipelineStep
{
    public Task Run(CancellationToken cancellationToken = default) =>
        dotnet.Restore(new RestoreArgs(), cancellationToken);
}
