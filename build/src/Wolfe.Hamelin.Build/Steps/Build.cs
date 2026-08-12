using System.ComponentModel;
using System.Text.RegularExpressions;
using Hamelin;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Commands;
using Wolfe.Hamelin.Reporting;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Build Solution")]
public partial class Build(IOptions<BuildOptions> options, ICommandRunner commands, IBuildReport report) : IPipelineStep
{
    private const int MaxDiagnostics = 30;

    public async Task Run(CancellationToken cancellationToken = default)
    {
        var dotnetBuild = Command
            .Create("dotnet")
            .WithArguments("build", "--no-restore")
            .AndArguments("--configuration", options.Value.Configuration);
        var result = await commands.Run(dotnetBuild, cancellationToken);
        if (result.IsSuccess)
        {
            return;
        }

        var section = report.Section("Build").Failure("The solution failed to build.");
        var diagnostics = ExtractDiagnostics(result.StandardOutput);
        if (diagnostics.Count > 0)
        {
            section.Details("Compiler output", $"```\n{string.Join('\n', diagnostics)}\n```");
        }

        throw new Exception("Build failed.");
    }

    private static IReadOnlyList<string> ExtractDiagnostics(string output)
    {
        var diagnostics = output
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => DiagnosticLine().IsMatch(l))
            .Distinct()
            .ToList();

        if (diagnostics.Count > MaxDiagnostics)
        {
            var omitted = diagnostics.Count - MaxDiagnostics;
            diagnostics = [.. diagnostics.Take(MaxDiagnostics), $"…and {omitted} more"];
        }

        return diagnostics;
    }

    [GeneratedRegex(@":\s(?:error|warning)\s\w+\d*:")]
    private static partial Regex DiagnosticLine();
}
