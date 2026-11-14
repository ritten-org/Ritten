using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.Npm.Steps;

/// <summary>
/// Fails when the project's <c>test</c> script does.
/// </summary>
/// <param name="npm">The npm client.</param>
/// <param name="report">The build report.</param>
[Step("npm test", StepKind.Check)]
public class NpmTest(INpm npm, IWorkflowReport report)
{
    /// <summary>
    /// Tests the project.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(NpmProject project, CancellationToken cancellationToken = default)
    {
        var result = await npm.Run(project, "test", cancellationToken);
        if (result.Succeeded)
        {
            report.Section(SectionName.Tests).Success("Every test passed.");
            return StepResult.Successful;
        }

        report.Section(SectionName.Tests).Failure(NpmOutput.Fenced(result.Output));
        return StepResult.Failed("Tests failed.");
    }
}
