using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.Npm.Steps;

/// <summary>
/// Fails when the project's <c>lint</c> script does: its linters and formatting checks, which check and never fix.
/// </summary>
/// <param name="npm">The npm client.</param>
/// <param name="report">The build report.</param>
[Step("npm lint", StepKind.Check)]
public class NpmLint(INpm npm, IWorkflowReport report)
{
    /// <summary>
    /// Lints the project.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(NpmProject project, CancellationToken cancellationToken = default)
    {
        var result = await npm.Run(project, "lint", cancellationToken);
        if (result.Succeeded)
        {
            report.Section(SectionName.Linting).Success("Linting passed.");
            return StepResult.Successful;
        }

        report.Section(SectionName.Linting).Failure(NpmOutput.Fenced(result.Output));
        return StepResult.Failed("Linting failed.");
    }
}
