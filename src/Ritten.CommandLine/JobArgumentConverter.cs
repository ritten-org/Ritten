using Ritten.Engine.Workflows;

namespace Ritten.CommandLine;

/// <summary>
/// Renders each of a job's arguments as an option, and remembers how to read it back.
/// </summary>
internal sealed class JobArgumentConverter : IJobArgumentConverter<JobArgumentOption>
{
    private JobArgumentConverter() { }

    /// <summary>
    /// The singleton instance of this converter.
    /// </summary>
    public static JobArgumentConverter Instance { get; } = new();

    /// <inheritdoc />
    public JobArgumentOption Convert<T>(JobArgument<T> argument)
    {
        var option = argument.ToOption();
        return new JobArgumentOption(option, (parse, values) => values.Set(argument, parse.GetValue(option)));
    }
}
