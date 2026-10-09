using System.CommandLine;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Engine.Workflows;

namespace Ritten.CommandLine;

/// <summary>
/// Builds the standard command for a job, for a host that assembles its own command line.
/// </summary>
public static class JobCommand
{
    /// <summary>
    /// Creates the command for a job.
    /// </summary>
    /// <param name="job">The job the command runs.</param>
    /// <param name="flags">The flags every job takes, already added to a parent command.</param>
    /// <param name="run">Runs the job with what the command line asked of it.</param>
    public static Command Create(IJob job, WorkflowFlags flags, Func<RunJobArgs, ParseResult, CancellationToken, Task<ExitCode>> run)
    {
        var command = new Command(job.Name, job.Description);
        List<(JobOption Declared, Option Option)> options = [.. job.Options.Select(option => (option, option.ToOption()))];
        foreach (var (_, option) in options)
        {
            command.Options.Add(option);
        }

        command.SetAction(async (parseResult, ct) =>
        {
            var runOptions = flags.RunOptions(parseResult);
            var args = new RunJobArgs(job.Name)
            {
                LogLevel = runOptions.LogLevel,
                DryRun = runOptions.DryRun,
                AutoApprove = runOptions.AutoApprove,
                Options = options.ToDictionary(o => o.Declared, o => Given(parseResult, o.Option))
            };

            return await run(args, parseResult, ct);
        });

        return command;
    }

    private static object? Given(ParseResult parseResult, Option option) => option switch
    {
        Option<bool> flag => parseResult.GetValue(flag),
        Option<object> value => parseResult.GetValue(value),
        _ => null
    };
}
