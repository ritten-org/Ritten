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
        // A job that runs without a project has no predetermined workflow.
        var workflow = job.RequiresProject ? null : WorkflowOption();
        var command = CommandLine.JobCommand.Create(job, flags, async (args, parseResult, ct) =>
        {
            var selection = await application.SelectWorkflow(
                Environment.CurrentDirectory,
                workflow is null ? null : parseResult.GetValue(workflow),
                ct
            );

            return await application.Run(selection, args, ct);
        });

        if (workflow is not null)
        {
            command.Options.Add(workflow);
        }

        return command;
    }
}
