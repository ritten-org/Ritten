using Microsoft.Extensions.DependencyInjection;

namespace Ritten.Engine.Workflows;

/// <summary>
/// The workflows a host registers, in the order they are recognised in.
/// </summary>
public sealed class WorkflowRegistry
{
    private readonly List<ServiceDescriptor> _workflows = [];

    /// <summary>
    /// Registers a workflow.
    /// </summary>
    /// <param name="workflow">The workflow to register.</param>
    public WorkflowRegistry Add(IWorkflow workflow)
    {
        _workflows.Add(ServiceDescriptor.Singleton(workflow));
        return this;
    }

    /// <summary>
    /// Registers a workflow by type, constructed by the application's services when it is built.
    /// </summary>
    /// <typeparam name="T">The workflow type to construct and register.</typeparam>
    public WorkflowRegistry Add<T>() where T : class, IWorkflow
    {
        _workflows.Add(ServiceDescriptor.Singleton<IWorkflow, T>());
        return this;
    }

    /// <summary>
    /// Registers every workflow with the application's services.
    /// </summary>
    internal void Register(IServiceCollection services)
    {
        foreach (var workflow in _workflows)
        {
            services.Add(workflow);
        }
    }
}
