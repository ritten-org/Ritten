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
    /// The option that names a workflow, for jobs that run without one.
    /// </summary>
    private static Option<string> WorkflowOption() => new($"--{WorkflowArguments.Workflow}")
    {
        Description = "The workflow to run. Recognised from what's in the project when omitted."
    };

    /// <summary>
    /// Builds the command for a single job.
    /// </summary>
    private static Command JobCommand(IJob job, WorkflowFlags flags, WorkflowApplication application)
    {
        var command = new Command(job.Name, job.Description);
        List<JobArgumentOption> arguments = [.. job.Arguments.Select(argument => argument.Convert(JobArgumentConverter.Instance))];
        foreach (var argument in arguments)
        {
            command.Options.Add(argument.Option);
        }

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var builder = new JobArgumentsBuilder(parseResult);
            foreach (var argument in arguments)
            {
                builder.Add(argument);
            }
            var jobArgs = builder.Build();

            var args = new RunJobArgs(job.Name)
            {
                LogLevel = flags.LogLevel(parseResult),
                DryRun = parseResult.GetValue(flags.DryRun),
                AutoApprove = parseResult.GetValue(flags.AutoApprove),
                Arguments = jobArgs
            };

            return await application.Run(args, cancellationToken);
        });

        return command;
    }
}
