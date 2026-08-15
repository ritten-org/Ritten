using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.DotNet;
using Ritten.Reporting;

namespace Ritten.Pipelines.DotNet.Steps;

/// <summary>
/// Builds the solution, reporting compiler diagnostics when the build fails.
/// </summary>
/// <param name="options">The pipeline's build options.</param>
/// <param name="dotnet">The dotnet client.</param>
/// <param name="report">The build report.</param>
[Step("dotnet build", StepKind.Work)]
public class DotnetBuild(IOptions<DotNetOptions> options, IDotNet dotnet, IBuildReport report)
{
    private const int MaxDiagnostics = 30;

    /// <summary>
    /// Builds the solution.
    /// </summary>
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        var result = await dotnet.Build(
            new BuildArgs { Configuration = options.Value.Configuration, NoRestore = true },
            cancellationToken);
        if (result.Succeeded)
        {
            return StepResult.Successful;
        }

        var section = report.Section("Build").Failure("The solution failed to build.");
        if (result.Diagnostics.Count == 0)
        {
            return StepResult.Failed("The solution failed to build. Re-run with --verbose to see the compiler output.");
        }

        var diagnostics = result.Diagnostics.Select(d => d.ToString()).ToList();
        if (diagnostics.Count > MaxDiagnostics)
        {
            var omitted = diagnostics.Count - MaxDiagnostics;
            diagnostics = [.. diagnostics.Take(MaxDiagnostics), $"…and {omitted} more"];
        }

        section.Details("Compiler output", $"```\n{string.Join('\n', diagnostics)}\n```");

        // The terminal gets what the pull request comment gets; the command output that
        // produced it is hidden unless someone asks for --verbose.
        return StepResult.Failed(diagnostics.Select(d => new Error(d)));
    }
}
