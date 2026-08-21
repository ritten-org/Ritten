using Ritten.Contracts;
using Ritten.Engine.Workflows;

namespace Ritten.Engine.Rules;

/// <summary>
/// Requires a job that says it deploys to actually contain a deploy step, and one that says
/// it only checks to contain none.
/// </summary>
public sealed class DeployJobsPublish : IJobRule
{
    /// <inheritdoc />
    public IEnumerable<Error> Check(IJob job)
    {
        var deploys = job.Steps.Where(step => step.Kind == StepKind.Publish).ToList();
        switch (job.Kind)
        {
            case JobKind.Deploy when deploys.Count == 0:
                yield return Result.Error($"The {job.Name} job publishes, but none of its steps do. Give it a publish step, or declare it another kind.");
                break;

            case JobKind.Check:
                foreach (var step in deploys)
                {
                    yield return Result.Error($"The {job.Name} job only checks, but '{step.Name}' publishes. A job that runs on every change must not release.");
                }

                break;
        }
    }
}
