using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Options;
using Ritten.DotNet;
using Ritten.Reporting;

namespace Ritten.Pipelines.DotNet;

/// <summary>
/// Builds the solution, reporting compiler diagnostics when the build fails.
/// </summary>
/// <param name="options">The pipeline's build options.</param>
/// <param name="dotnet">The dotnet client.</param>
/// <param name="report">The build report.</param>
[DisplayName("Build .NET Solution")]
public class DotNetBuild(IOptions<DotNetOptions> options, IDotNet dotnet, IBuildReport report) : IPipelineStep
{
    private const int MaxDiagnostics = 30;

    /// <inheritdoc />
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var result = await dotnet.Build(
            new BuildArgs { Configuration = options.Value.Configuration, NoRestore = true },
            cancellationToken);
        if (result.Succeeded)
        {
            return;
        }

        var section = report.Section("Build").Failure("The solution failed to build.");
        if (result.Diagnostics.Count > 0)
        {
            var diagnostics = result.Diagnostics.Select(d => d.ToString()).ToList();
            if (diagnostics.Count > MaxDiagnostics)
            {
                var omitted = diagnostics.Count - MaxDiagnostics;
                diagnostics = [.. diagnostics.Take(MaxDiagnostics), $"…and {omitted} more"];
            }

            section.Details("Compiler output", $"```\n{string.Join('\n', diagnostics)}\n```");
        }

        throw new Exception("Build failed.");
    }
}
