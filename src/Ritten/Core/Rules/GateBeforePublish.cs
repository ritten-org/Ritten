using Ritten.Contracts;

namespace Ritten.Core.Rules;

/// <summary>
/// Requires a gate before the first publish step: nothing irreversible happens until something
/// has had the chance to stop the job.
/// </summary>
public sealed class GateBeforePublish : IJobRule
{
    /// <inheritdoc />
    public IEnumerable<Error> Check(IReadOnlyList<JobStep> steps)
    {
        var firstPublish = steps.FirstOrDefault(s => s.Kind == StepKind.Publish);
        if (firstPublish is null)
        {
            yield break;
        }

        if (steps.TakeWhile(s => s != firstPublish).All(s => s.Kind != StepKind.Gate))
        {
            yield return Result.Error($"'{firstPublish.Name}' is irreversible, but no gate runs before it. Add a gate ahead of the first publish step.");
        }
    }
}
