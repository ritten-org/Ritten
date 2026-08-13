using Ritten.Core.Logging;
using Microsoft.Extensions.Logging;

namespace Ritten.Core.Extensions;

internal static class ConsoleFormatterExtensions
{
    extension(ILoggingBuilder builder)
    {
        public ILoggingBuilder AddPipelineConsoleFormatter(Action<PipelineConsoleFormatterOptions> configure) =>
            builder.AddConsole(options => options.FormatterName = PipelineConsoleFormatter.FormatterName)
                .AddConsoleFormatter<PipelineConsoleFormatter, PipelineConsoleFormatterOptions>(configure);

        public ILoggingBuilder AddPipelineConsoleFormatter() =>
            builder.AddConsole(options => options.FormatterName = PipelineConsoleFormatter.FormatterName)
                .AddConsoleFormatter<PipelineConsoleFormatter, PipelineConsoleFormatterOptions>();
    }
}
