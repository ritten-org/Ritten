using System.Text.Json;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Core.Runtimes;
using Ritten.Reporting;
using Spectre.Console;

namespace Ritten.Tests.Core.Helpers;

internal static class PipelineHostBuilderHelpers
{
    /// <summary>
    /// An environment with everything set. Tests supply their own rather than reading the process
    /// environment, which on a GitHub Actions runner is anything but empty.
    /// </summary>
    public static Func<string, string?> Complete { get; } = _ => "set";

    /// <summary>An environment with nothing set.</summary>
    public static Func<string, string?> Empty { get; } = _ => null;

    public static PipelineHostBuilder Create(
        string pipelineName = "Test",
        Func<string, string?>? environment = null,
        bool dryRun = false,
        IPipelineLog? log = null,
        string settings = "{}",
        RuntimeRegistry? runtimes = null)
    {
        var builder = new PipelineHostBuilder(
                new RittenProject { Directory = Path.GetTempPath(), Settings = JsonSerializer.Deserialize<JsonElement>(settings) },
                (runtimes ?? new RuntimeRegistry()).Detect(environment ?? Complete).Value.ShouldNotBeNull(),
                new SpectrePipelineConsole(AnsiConsole.Console, PipelineLogLevel.Detail))
            .WithPipelineLabel(pipelineName)
            .WithDryRun(dryRun);
        return log is null ? builder : builder.WithLog(log);
    }
}
