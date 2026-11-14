using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.Npm.Steps;

/// <summary>
/// Fails when the project's <c>build</c> script does: its type check and bundle.
/// </summary>
/// <param name="npm">The npm client.</param>
/// <param name="report">The build report.</param>
[Step("npm build", StepKind.Work)]
public class NpmBuild(INpm npm, IWorkflowReport report)
{
    /// <summary>
    /// Builds the project.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(NpmProject project, CancellationToken cancellationToken = default)
    {
        var result = await npm.Run(project, "build", cancellationToken);
        if (result.Succeeded)
        {
            report.Section(SectionName.Build).Success("The build succeeded.");
            return StepResult.Successful;
        }

        report.Section(SectionName.Build).Failure(NpmOutput.Fenced(result.Output));
        return StepResult.Failed("The build failed.");
    }
}
