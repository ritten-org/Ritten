using Ritten.Contracts;
using Ritten.Core;
using Ritten.Reporting;
using Spectre.Console;

namespace Ritten.Tests.Core.Helpers;

internal static class PipelineHostBuilderHelpers
{
    public static PipelineHostBuilder Create(string job, string pipelineName = "Test") =>
        new(
            new RittenProject { Directory = Path.GetTempPath() },
            pipelineName,
            job,
            new SpectreProgressReporter(AnsiConsole.Console, PipelineLogLevel.Detail));
}
