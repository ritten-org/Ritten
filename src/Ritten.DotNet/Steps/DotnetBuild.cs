using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Builds the solution, reporting compiler diagnostics when the build fails.
/// </summary>
/// <param name="dotnet">The dotnet client.</param>
/// <param name="report">The build report.</param>
[Step("dotnet build", StepKind.Work)]
public class DotnetBuild(IDotNet dotnet, IWorkflowReport report)
{
    /// <summary>
    /// Builds the solution.
    /// </summary>
    /// <param name="build">What to build, and how.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(DotNetBuildSettings build, CancellationToken cancellationToken = default)
    {
        var result = await dotnet.Build(
            new BuildArgs { Configuration = build.Configuration, NoRestore = true },
            cancellationToken);
        if (result.Succeeded)
        {
            return StepResult.Successful;
        }

        var section = report.Section(SectionName.Build).Failure("The solution failed to build.");
        if (result.Diagnostics.Count == 0)
        {
            return StepResult.Failed("The solution failed to build. Re-run with --verbose to see the compiler output.");
        }

        return section.FailWithDiagnostics("Compiler output", result.Diagnostics);
    }
}
