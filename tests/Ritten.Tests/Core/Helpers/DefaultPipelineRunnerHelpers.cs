using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ritten.Contracts;
using Ritten.Contracts.Hooks;
using Ritten.Core.Runner;
using Ritten.Core.Steps;

namespace Ritten.Tests.Core.Helpers;

internal static class DefaultPipelineRunnerHelpers
{
    public static DefaultPipelineRunner CreateRunner(
        IPipelineStep[]? steps = null,
        IPrePipelineHook[]? prePipelineHooks = null,
        IPostPipelineHook[]? postPipelineHooks = null,
        IPipelineContext? context = null,
        ILogger<DefaultPipelineRunner>? logger = null
    )
    {
        logger ??= Substitute.For<ILogger<DefaultPipelineRunner>>();
        context ??= Substitute.For<IPipelineContext>();

        var stepProvider = Substitute.For<IPipelineStepProvider>();
        stepProvider.GetSteps().Returns(steps ?? []);

        var services = new ServiceCollection()
            .AddSingleton(stepProvider)
            .AddSingleton(context);

        foreach (var hook in prePipelineHooks ?? [])
        {
            services.AddScoped<IPrePipelineHook>(_ => hook);
        }
        foreach (var hook in postPipelineHooks ?? [])
        {
            services.AddScoped<IPostPipelineHook>(_ => hook);
        }

        var provider = services.BuildServiceProvider();
        var scopeFactory = ServiceScopeHelpers.CreateScopeFactory(provider);

        return new DefaultPipelineRunner(logger, scopeFactory);
    }
}
