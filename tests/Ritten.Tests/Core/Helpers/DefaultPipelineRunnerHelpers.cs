using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ritten.Contracts;
using Ritten.Core.Runner;
using Ritten.Core.Steps;

namespace Ritten.Tests.Core.Helpers;

internal static class DefaultPipelineRunnerHelpers
{
    public static DefaultPipelineRunner CreateRunner(
        IPipelineStep[]? steps = null,
        IProgressReporter[]? reporters = null,
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

        foreach (var reporter in reporters ?? [])
        {
            services.AddScoped<IProgressReporter>(_ => reporter);
        }

        var provider = services.BuildServiceProvider();
        var scopeFactory = ServiceScopeHelpers.CreateScopeFactory(provider);

        return new DefaultPipelineRunner(logger, scopeFactory);
    }
}
