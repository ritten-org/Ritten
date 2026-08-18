using Ritten.Contracts;

namespace Ritten.Engine.Rules;

/// <summary>
/// Requires checks to run before any publish step.
/// </summary>
public sealed class CheckBeforePublish : IJobRule
{
    /// <inheritdoc />
    public IEnumerable<Error> Check(IJob job)
    {
        var published = false;
        foreach (var step in job.Steps)
        {
            if (step.Kind == StepKind.Publish)
            {
                published = true;
            }
            else if (published && step.Kind == StepKind.Check)
            {
                yield return Result.Error($"'{step.Name}' checks after the job has already started publishing. Move checks ahead of the publish steps.");
            }
        }
    }
}
