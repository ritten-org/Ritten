using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.OpenTofu.Steps;

/// <summary>
/// Fails when the root module is not formatted, naming the files that are not.
/// </summary>
/// <param name="tofu">The OpenTofu client.</param>
/// <param name="report">The build report.</param>
[Step("tofu fmt", StepKind.Check)]
public class TofuFormatCheck(IOpenTofu tofu, IWorkflowReport report)
{
    /// <summary>
    /// Checks the root module's formatting.
    /// </summary>
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        if (await tofu.VerifyFormatting(cancellationToken) is not { } unformatted)
        {
            report.Section(SectionName.Formatting).Success("Every file is formatted.");
            return StepResult.Successful;
        }

        report.Section(SectionName.Formatting)
            .Failure($"{unformatted.Count} file(s) need formatting. Run `tofu fmt -recursive` to fix them:\n{string.Join('\n', unformatted.Select(file => $"- `{file}`"))}");
        return StepResult.Failed("The root module is not formatted.");
    }
}
