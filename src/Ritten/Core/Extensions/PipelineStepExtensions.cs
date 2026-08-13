using System.ComponentModel;
using System.Reflection;
using Ritten.Contracts;

namespace Ritten.Core.Extensions;

internal static class PipelineStepExtensions
{
    extension(IPipelineStep step)
    {
        public string GetDisplayName()
        {
            var displayNameAttribute = step.GetType().GetCustomAttribute<DisplayNameAttribute>();
            return displayNameAttribute?.DisplayName ?? step.GetType().Name;
        }
    }
}
