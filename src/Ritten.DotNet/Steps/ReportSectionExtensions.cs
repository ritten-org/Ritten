using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Reporting;

namespace Ritten.DotNet.Steps;

internal static class ReportSectionExtensions
{
    private const int MaxDiagnostics = 30;

    extension(ReportSection section)
    {
        /// <summary>
        /// Adds the diagnostics to the section as a details block and fails the step with them.
        /// </summary>
        /// <param name="summary">A title for the details block.</param>
        /// <param name="diagnostics">The diagnostics the command produced.</param>
        public StepResult FailWithDiagnostics(string summary, IReadOnlyList<DotNetDiagnostic> diagnostics)
        {
            var lines = diagnostics.Select(d => d.ToString()).ToList();
            if (lines.Count > MaxDiagnostics)
            {
                var omitted = lines.Count - MaxDiagnostics;
                lines = [.. lines.Take(MaxDiagnostics), $"…and {omitted} more"];
            }

            section.Details(summary, $"```\n{string.Join('\n', lines)}\n```");
            return StepResult.Failed(lines.Select(Error.From));
        }
    }
}
