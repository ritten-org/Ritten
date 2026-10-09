using Ritten.Contracts;
using Ritten.Engine.Workflows;

namespace Ritten.Engine.Rules;

/// <summary>
/// Requires steps to run in produce-then-consume order.
/// </summary>
public sealed class ProduceBeforeConsume : IJobRule
{
    /// <inheritdoc />
    public IEnumerable<Error> Check(IJob job)
    {
        // The job's arguments are in the state before its first step.
        HashSet<Type> produced = [.. job.Inputs];
        foreach (var step in job.Steps)
        {
            foreach (var parameter in step.Requires)
            {
                if (!produced.Contains(parameter))
                {
                    yield return Result.Error($"'{step.Name}' needs a {parameter.Name}, which no earlier step produces.");
                }
            }

            if (step.Produces is { } type)
            {
                produced.Add(type);
            }
        }
    }
}
