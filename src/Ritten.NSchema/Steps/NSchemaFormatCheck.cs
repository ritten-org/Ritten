using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.NSchema.Steps;

/// <summary>
/// Fails when the project's schema is not formatted, naming the files that are not.
/// </summary>
/// <param name="nschema">The NSchema client.</param>
/// <param name="report">The build report.</param>
[Step("nschema format", StepKind.Check)]
public class NSchemaFormatCheck(INSchema nschema, IWorkflowReport report)
{
    /// <summary>
    /// Checks the project's formatting.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<StepResult> Run(NSchemaProject project, CancellationToken cancellationToken = default)
    {
        if (await nschema.VerifyFormatting(project, cancellationToken) is not { } unformatted)
        {
            report.Section(SectionName.Formatting).Success("Every schema file is formatted.");
            return StepResult.Successful;
        }

        report.Section(SectionName.Formatting)
            .Failure($"{unformatted.Count} schema file(s) need formatting. Run `nschema format` to fix them:\n{string.Join('\n', unformatted.Select(file => $"- `{file}`"))}");
        return StepResult.Failed("The schema is not formatted.");
    }
}
