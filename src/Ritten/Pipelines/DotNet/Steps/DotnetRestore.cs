using Ritten.Contracts;
using Ritten.DotNet;

namespace Ritten.Pipelines.DotNet.Steps;

/// <summary>
/// Runs <c>dotnet restore</c>.
/// </summary>
/// <param name="dotnet">The dotnet client.</param>
[Step("dotnet restore", StepKind.Work)]
public class DotnetRestore(IDotNet dotnet)
{
    /// <summary>
    /// Restores the solution's packages.
    /// </summary>
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        await dotnet.Restore(new RestoreArgs(), cancellationToken);
        return StepResult.Successful;
    }
}
