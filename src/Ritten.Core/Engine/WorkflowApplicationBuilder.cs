using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Engine.DryRun;
using Ritten.Engine.Runtimes;
using Ritten.Engine.Workflows;
using Ritten.Reporting;

namespace Ritten.Engine;

/// <summary>
/// Configures a <see cref="WorkflowApplication"/>.
/// </summary>
public sealed class WorkflowApplicationBuilder : IWorkflowBuilder
{
    internal WorkflowApplicationBuilder()
    {
    }

    /// <summary>
    /// The name of the file that marks a project's root and declares its workflow.
    /// </summary>
    public string ProjectFileName { get; set; } = RittenProject.DefaultFileName;

    /// <summary>
    /// The workflows the application can run.
    /// </summary>
    public WorkflowRegistry Workflows { get; } = new();

    /// <summary>
    /// The runtimes the application can find itself running in.
    /// </summary>
    public RuntimeRegistry Runtimes { get; } = new();

    /// <summary>
    /// Services registered for every job of every workflow.
    /// </summary>
    public IServiceCollection Services { get; } = new ServiceCollection();

    /// <summary>
    /// The decorators applied during a dry run, for the shared services.
    /// </summary>
    public DecoratorRegistry Decorators { get; } = new();

    /// <summary>
    /// Builds and validates the workflow application.
    /// </summary>
    public Result<WorkflowApplication> Build()
    {
        List<Error> model = [.. Workflows.Validate(), .. Runtimes.Validate()];
        if (model.Count > 0)
        {
            // Narrated here, so a host's entry point can map the failure straight to an exit
            // code without owning any reporting of its own.
            WorkflowApplication.EngineConsole().Errors(model);
            return model;
        }

        return new WorkflowApplication(Workflows, Runtimes, [.. Services], Decorators, ProjectFileName);
    }
}
