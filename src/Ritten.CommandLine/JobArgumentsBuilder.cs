using System.CommandLine;
using Ritten.Engine.Workflows;

namespace Ritten.CommandLine;

/// <summary>
/// Collects the values a run was invoked with, keyed by the declarations they were given for.
/// </summary>
internal sealed class JobArgumentsBuilder(ParseResult parseResult)
{
    private readonly Dictionary<JobArgument, object?> _values = [];

    /// <summary>
    /// Records the value read for an argument. A null value is the caller supplying none.
    /// </summary>
    /// <typeparam name="T">The type the argument reads as.</typeparam>
    /// <param name="argument">The declaration the value was given for.</param>
    /// <param name="value">The value read.</param>
    public JobArgumentsBuilder Set<T>(JobArgument<T> argument, T? value)
    {
        if (value is not null)
        {
            _values[argument] = value;
        }

        return this;
    }

    /// <summary>
    /// Builds the values, as the job will read them.
    /// </summary>
    public JobArguments Build() => new(_values);

    /// <summary>
    /// Adds the given argument to the builder.
    /// </summary>
    public void Add(JobArgumentOption argument)
    {
        argument.Parse(parseResult, this);
    }
}
