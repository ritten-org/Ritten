namespace Ritten.Core.Steps;

internal interface IPipelineStepCollection
{
    IReadOnlyList<Type> Steps { get; }
    void Add<T>() where T : class;
    void AddStep(Type stepType);
}
