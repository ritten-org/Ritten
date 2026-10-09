using Ritten.Engine.Workflows;
using Ritten.Reporting;

namespace Ritten.Engine.Runs;

/// <summary>
/// One run of a job.
/// </summary>
/// <param name="Workflow">The workflow, and the project it runs in.</param>
/// <param name="Job">The job being run.</param>
/// <param name="Arguments">The job's arguments, put into the state before its first step.</param>
/// <param name="Options">How the job runs.</param>
/// <param name="Console">The console narrative the run renders through.</param>
internal sealed record Run(SelectedWorkflow Workflow, IJob Job, object Arguments, RunOptions Options, IWorkflowConsole Console);