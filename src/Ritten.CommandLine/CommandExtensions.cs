using System.CommandLine;
using Ritten.Engine;
using Ritten.Engine.Workflows;

namespace Ritten.CommandLine;

/// <summary>
/// Contains extension methods for <see cref="Command"/>.
/// </summary>
public static class CommandExtensions
{
    extension(Command command)
    {
        /// <summary>
        /// Configures this command as the Ritten workflow's root.
        /// </summary>
        /// <param name="application">the workflow to install.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task InstallRitten(WorkflowApplication application, CancellationToken ct = default)
        {
            var flags = new WorkflowFlags();
            foreach (var flag in flags.Options)
            {
                command.Options.Add(flag);
            }

            var jobs = application.ResolveJobs(Environment.CurrentDirectory, ct);
            foreach (var job in await jobs)
            {
                command.Subcommands.Add(JobCommand(job, flags, application));
            }
        }
    }

    /// <summary>
    /// Builds the command for a single job.
    /// </summary>
    private static Command JobCommand(IJob job, WorkflowFlags flags, WorkflowApplication application)
    {
        var command = new Command(job.Name, job.Description);
        List<Option> arguments = [.. job.Arguments.Select(argument => argument.ToOption())];
        foreach (var argument in arguments)
        {
            command.Options.Add(argument);
        }

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var builder = new JobArgumentsBuilder();
            foreach (var argument in arguments)
            {
                builder.Add(argument, parseResult);
            }

            var args = new RunJobArgs(job.Name)
            {
                LogLevel = flags.LogLevel(parseResult),
                DryRun = parseResult.GetValue(flags.DryRun),
                AutoApprove = parseResult.GetValue(flags.AutoApprove),
                Arguments = builder.Build()
            };

            return await application.Run(args, cancellationToken);
        });

        return command;
    }
}
