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
    }
}
