using System.Text;
using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.OpenTofu.Steps;

/// <summary>
/// Works out what the change would do to the infrastructure, and puts it in the report.
/// </summary>
/// <remarks>
/// Stops the job when there is nothing to apply. A plan that found no changes has answered the
/// question the job was asking, and an apply behind it would only prove the same thing more
/// slowly — so the steps after this one run when, and only when, there is something to do.
/// </remarks>
/// <param name="tofu">The OpenTofu client.</param>
/// <param name="report">The build report.</param>
/// <param name="log">The run's log.</param>
[Step("tofu plan", StepKind.Check)]
public class TofuPlan(IOpenTofu tofu, IWorkflowReport report, IWorkflowLog log)
{
    /// <summary>
    /// Plans the root module.
    /// </summary>
    public async Task<StepResult> Run(CancellationToken cancellationToken = default)
    {
        var plan = await tofu.Plan(cancellationToken);
        if (!plan.HasChanges)
        {
            report.Section(SectionName.Infrastructure).Success("No changes. The infrastructure matches the configuration.");
            log.Status("Nothing to apply.");
            return StepResult.NothingToDo;
        }

        report.Section(SectionName.Infrastructure).Note(AsDiff(plan.Output));
        return StepResult.Successful;
    }

    /// <summary>
    /// The plan as a diff block, so a reader sees what moves rather than a wall of output.
    /// </summary>
    private static string AsDiff(string plan)
    {
        var formatted = new StringBuilder("```diff\n");
        foreach (var line in plan.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            // OpenTofu already marks each line with what it does to the resource; this only
            // promotes that mark to the first column, where a diff renderer will colour it.
            var marker = line.TrimStart() switch
            {
                ['-', ..] => '-',
                ['+', ..] => '+',
                ['~', ..] => '!',
                ['#', ..] => '#',
                _ => ' '
            };
            formatted.Append(marker).AppendLine(line);
        }

        return formatted.AppendLine("```").ToString();
    }
}
