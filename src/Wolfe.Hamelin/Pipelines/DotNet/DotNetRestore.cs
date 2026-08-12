using System.ComponentModel;
using Hamelin;
using Wolfe.Hamelin.DotNet;

namespace Wolfe.Hamelin.Pipelines.DotNet;

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
