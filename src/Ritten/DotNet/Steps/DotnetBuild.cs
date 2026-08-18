using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Builds the solution, reporting compiler diagnostics when the build fails.
/// </summary>
/// <param name="options">The workflow's build options.</param>
/// <param name="dotnet">The dotnet client.</param>
/// <param name="report">The build report.</param>
[Step("dotnet build", StepKind.Work)]
public class DotnetBuild(IOptions<DotNetOptions> options, IDotNet dotnet, IBuildReport report)
{
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

        return section.FailWithDiagnostics("Compiler output", result.Diagnostics);
    }
}
