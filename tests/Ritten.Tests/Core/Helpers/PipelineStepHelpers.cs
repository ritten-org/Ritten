using Ritten.Contracts;

namespace Ritten.Tests.Core.Helpers;

/// <summary>
/// A configurable step for engine tests. The runner resolves step instances by their type, so
/// tests that need several independent steps in one job use the A/B/C subclasses.
/// </summary>
public class TestStep : IPipelineStep
{
    /// <summary>What the step does when run; successful when not set.</summary>
    public Func<CancellationToken, Task<StepResult>>? OnRun { get; set; }

    /// <summary>A shared journal the step appends itself to, for ordering assertions.</summary>
    public List<object>? Journal { get; set; }

    /// <summary>How many times the step has run.</summary>
    public int Runs { get; private set; }

    public async Task<StepResult> Run(CancellationToken cancellationToken)
    {
        Runs++;
        Journal?.Add(this);
        return OnRun is null ? StepResult.Successful : await OnRun(cancellationToken);
    }
}

public sealed class TestStepA : TestStep;

public sealed class TestStepB : TestStep;

public sealed class TestStepC : TestStep;
