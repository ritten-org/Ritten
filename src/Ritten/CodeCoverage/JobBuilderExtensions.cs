using Ritten.CodeCoverage.Steps;
using Ritten.Core;

namespace Ritten.CodeCoverage;

internal static class JobBuilderExtensions
{
    extension(IJobBuilder builder)
    {
        /// <summary>
        /// Registers the code coverage steps.
        /// </summary>
        public IJobBuilder UseCoverage()
        {
            return builder
                .UseStep<ReadCoverage>()
                .UseStep<CoverageValidate>();
        }
    }
}
