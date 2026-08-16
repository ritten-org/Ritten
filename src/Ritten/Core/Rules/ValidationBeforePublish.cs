using Ritten.Contracts;

namespace Ritten.Core.Rules;

/// <summary>
/// Requires validations to run before any publish step.
/// </summary>
public sealed class ValidationBeforePublish : IJobRule
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
            else if (published && step.Kind == StepKind.Validation)
            {
                yield return Result.Error($"'{step.Name}' validates after the job has already started publishing. Move validations ahead of the publish steps.");
            }
        }
    }
}
