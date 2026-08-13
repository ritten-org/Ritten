using System.ComponentModel;
using Ritten.Contracts;
using Ritten.DotNet;

namespace Ritten.Pipelines.DotNet.Steps;

/// <summary>
/// Runs <c>dotnet restore</c>.
/// </summary>
/// <param name="dotnet">The dotnet client.</param>
[DisplayName("Restore .NET Dependencies")]
public class DotNetRestore(IDotNet dotnet) : IPipelineStep
{
    /// <inheritdoc />
    public Task Run(CancellationToken cancellationToken = default) =>
        dotnet.Restore(new RestoreArgs(), cancellationToken);
}
