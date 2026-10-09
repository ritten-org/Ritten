namespace Ritten.Engine.Runs;

/// <summary>
/// What one run is about, held by its scope; every run-scoped fact the engine registers is read from here.
/// </summary>
internal sealed class RunState
{
    private Run? _run;

    /// <summary>
    /// The run the scope belongs to.
    /// </summary>
    public Run Current => _run ?? throw new InvalidOperationException("A run's services were resolved before the run started.");

    /// <summary>
    /// Starts the scope's run.
    /// </summary>
    public void Start(Run run) => _run = run;
}