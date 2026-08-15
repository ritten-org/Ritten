using Ritten.Core;
using Ritten.DotNet.Steps;
using Ritten.Pipelines;

namespace Ritten.DotNet;

internal static class JobBuilderExtensions
{
    extension(IJobBuilder builder)
    {
        /// <summary>
        /// Registers the code coverage steps based on the given settings.
        /// </summary>
        public IJobBuilder UseCoverage(CoverageSettings? settings)
        {
            if (settings is not null)
            {
                builder
                    .UseStep<ReadCoverage>()
                    .UseStep<CoverageValidate>();
            }
            return builder;
        }
    }
}
