namespace Ritten.Core.Steps;

internal class PipelineStepCollection : IPipelineStepCollection
{
    private readonly List<Type> _steps = [];

    public IReadOnlyList<Type> Steps => _steps.AsReadOnly();

    public void Add<T>() where T : class
    {
        _steps.Add(typeof(T));
    }

    public void AddStep(Type stepType)
    {
        _steps.Add(stepType);
    }
}
