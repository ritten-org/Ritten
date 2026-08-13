using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts.Hooks;

namespace Ritten.Core.Extensions;

internal static class RittenServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPrePipelineHook<THook>()
            where THook : class, IPrePipelineHook
        {
            return services.AddTransient<IPrePipelineHook, THook>();
        }

        public IServiceCollection AddPostPipelineHook<THook>()
            where THook : class, IPostPipelineHook
        {
            return services.AddTransient<IPostPipelineHook, THook>();
        }

        public IServiceCollection AddPreStepHook<THook>()
            where THook : class, IPreStepHook
        {
            return services.AddTransient<IPreStepHook, THook>();
        }

        public IServiceCollection AddPostStepHook<THook>()
            where THook : class, IPostStepHook
        {
            return services.AddTransient<IPostStepHook, THook>();
        }
    }
}
