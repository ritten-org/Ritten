namespace Ritten.Core.Steps;

internal class PipelineStepTypes(IReadOnlyList<Type> steps)
{
    public IReadOnlyList<Type> Steps { get; } = steps;
}
