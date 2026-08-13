using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.Contracts.Hooks;

namespace Ritten.Core.Extensions;

internal static class RittenServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddStep<TPipelineStep>()
            where TPipelineStep : class, IPipelineStep
        {
            return services.AddTransient<TPipelineStep>();
        }

        public IServiceCollection AddStepsFromAssembly(Assembly assembly)
        {
            var stepType = typeof(IPipelineStep);
            var stepTypes = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsClass: true } && stepType.IsAssignableFrom(t));

            foreach (var type in stepTypes)
            {
                services.AddTransient(type);
            }

            return services;
        }

        public IServiceCollection AddStepsFromAssemblyContaining<TType>()
        {
            var assembly = typeof(TType).Assembly;
            return services.AddStepsFromAssembly(assembly);
        }

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

        public IServiceCollection AddHooksFromAssembly(Assembly assembly)
        {
            var allTypes = assembly.GetTypes();
            AddHooksOfType(typeof(IPrePipelineHook));
            AddHooksOfType(typeof(IPostPipelineHook));
            AddHooksOfType(typeof(IPreStepHook));
            AddHooksOfType(typeof(IPostStepHook));

            return services;

            void AddHooksOfType(Type hookType)
            {
                var hookTypes = allTypes
                    .Where(t => t is { IsAbstract: false, IsClass: true } && hookType.IsAssignableFrom(t));
                foreach (var type in hookTypes)
                {
                    services.AddTransient(hookType, type);
                }
            }
        }

        public IServiceCollection AddHooksFromAssemblyContaining<TType>()
        {
            var assembly = typeof(TType).Assembly;
            return services.AddHooksFromAssembly(assembly);
        }
    }
}
