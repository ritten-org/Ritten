using Ritten.Contracts;
using Ritten.Core;
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
        IPipelineLog? log = null) =>
        new(
            new RittenProject { Directory = Path.GetTempPath() },
            pipelineName,
            new SpectreProgressReporter(AnsiConsole.Console, PipelineLogLevel.Detail),
            dryRun,
            autoApprove: false,
            environment ?? Complete,
            log);
}
