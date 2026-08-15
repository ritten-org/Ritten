using Ritten.Contracts;

namespace Ritten.Core.Rules;

/// <summary>
/// Requires steps to run in produce-then-consume order.
/// </summary>
/// <param name="services">The container the job will run against, for the service fallback.</param>
public sealed class ProduceBeforeConsume(IServiceProvider services) : IJobRule
{
    /// <inheritdoc />
    public IEnumerable<Error> Check(IReadOnlyList<JobStep> steps)
    {
        HashSet<Type> produced = [];
        foreach (var step in steps)
        {
            foreach (var parameter in step.Requires)
            {
                if (!produced.Contains(parameter) && services.GetService(parameter) is null)
                {
                    yield return Result.Error($"'{step.Name}' needs a {parameter.Name}, which no earlier step produces and no registered service provides.");
                }
            }

            if (step.Produces is { } type)
            {
                produced.Add(type);
            }
        }
    }
}
