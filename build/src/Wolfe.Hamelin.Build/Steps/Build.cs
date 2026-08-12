using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.DotNet;
using Wolfe.Hamelin.Reporting;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Build Solution")]
public class Build(IOptions<BuildOptions> options, IDotNet dotnet, IBuildReport report) : IPipelineStep
{
    private const int MaxDiagnostics = 30;

    public async Task Run(CancellationToken cancellationToken = default)
    {
        var result = await dotnet.Build(
            new BuildArgs { Configuration = options.Value.Configuration, NoRestore = true },
            cancellationToken);
        if (result.Succeeded)
        {
            return;
        }

        var section = report.Section("Build").Failure("The solution failed to build.");
        if (result.Diagnostics.Count > 0)
        {
            var diagnostics = result.Diagnostics.Select(d => d.ToString()).ToList();
            if (diagnostics.Count > MaxDiagnostics)
            {
                var omitted = diagnostics.Count - MaxDiagnostics;
                diagnostics = [.. diagnostics.Take(MaxDiagnostics), $"…and {omitted} more"];
            }

            section.Details("Compiler output", $"```\n{string.Join('\n', diagnostics)}\n```");
        }

        throw new Exception("Build failed.");
    }
}
