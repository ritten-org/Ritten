using Microsoft.Extensions.Logging.Console;

namespace Ritten.Core.Logging;

internal class PipelineConsoleFormatterOptions : ConsoleFormatterOptions
{
    public PipelineConsoleFormatterOptions()
    {
        TimestampFormat = "o"; // ISO 8601
        ColorBehavior = LoggerColorBehavior.Default;
        IncludeScopes = true;
        IncludeStepNames = true;
    }

    public LoggerColorBehavior ColorBehavior { get; set; }

    public bool IncludeStepNames { get; set; }
}
