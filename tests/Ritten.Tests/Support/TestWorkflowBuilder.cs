using Microsoft.Extensions.DependencyInjection;
using Ritten.Engine;
using Ritten.Engine.DryRun;

namespace Ritten.Tests.Support;

/// <summary>
/// Somewhere for a module or runtime to register into, so a test can read back what it registered.
/// </summary>
internal sealed class TestWorkflowBuilder : IWorkflowBuilder
{
    public IServiceCollection Services { get; } = new ServiceCollection().AddOptions();

    public DecoratorRegistry Decorators { get; } = new();
}
