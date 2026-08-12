using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Commands;
using Wolfe.Hamelin.DotNet;
using Wolfe.Hamelin.Reporting;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Build Solution")]
public class Build(IOptions<BuildOptions> options, ICommandRunner commands, IDotNet dotnet, IBuildReport report) : IPipelineStep
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
        var diagnostics = dotnet.ParseDiagnostics(result.StandardOutput).Select(d => d.ToString()).ToList();
        if (diagnostics.Count > 0)
        {
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
