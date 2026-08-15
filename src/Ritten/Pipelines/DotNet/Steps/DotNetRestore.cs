using Ritten.Contracts;
using Ritten.DotNet;

namespace Ritten.Pipelines.DotNet.Steps;

/// <summary>
/// Runs <c>dotnet restore</c>.
/// </summary>
/// <param name="dotnet">The dotnet client.</param>
public class DotnetRestore(IDotNet dotnet) : IPipelineStep
{
    /// <inheritdoc />
    public string Name => "dotnet restore";

    /// <inheritdoc />
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        await dotnet.Restore(new RestoreArgs(), cancellationToken);
        return StepResult.Successful;
    }
}
