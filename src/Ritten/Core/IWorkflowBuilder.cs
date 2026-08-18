using Microsoft.Extensions.DependencyInjection;
using Ritten.Core.DryRun;

namespace Ritten.Core;

/// <summary>
/// Exposes a builder surface for components to configure themselves for a workflow run.
/// </summary>
public interface IWorkflowBuilder
{
    /// <summary>
    /// The service collection registrations land in.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// The dry-run decorators declared alongside the services.
    /// </summary>
    DecoratorRegistry Decorators { get; }
}
