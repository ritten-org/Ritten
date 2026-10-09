using Ritten.Contracts;
using Ritten.Engine.Workflows;
using Ritten.Workflows.DotNetTool;

namespace Ritten.Tests.Support;

/// <summary>
/// A job declared inline, over the given arguments: the steps and checks a test hands it, nothing more.
/// </summary>
internal class TestJob<TArguments>(
    string name = "verify",
    IReadOnlyList<Step>? steps = null,
    Action<ArgumentsValidator<TArguments>>? validate = null,
    JobKind kind = JobKind.Work,
    bool requiresProject = true,
    bool reports = true
) : Job<TArguments> where TArguments : class
{
    public override string Name => name;

    public override string Description => $"The {name} job, declared by a test.";

    public override JobKind Kind => kind;

    public override IReadOnlyList<Step> Steps { get; } = steps ?? [];

    public override bool RequiresProject => requiresProject;

    public override bool Reports => reports;

    protected override void Validate(ArgumentsValidator<TArguments> arguments) => validate?.Invoke(arguments);
}

/// <summary>
/// A job declared inline, over the .NET tool workflow's arguments.
/// </summary>
internal sealed class TestJob(
    string name = "verify",
    IReadOnlyList<Step>? steps = null,
    Action<ArgumentsValidator<DotNetToolArguments>>? validate = null,
    JobKind kind = JobKind.Work,
    bool requiresProject = true,
    bool reports = true
) : TestJob<DotNetToolArguments>(name, steps, validate, kind, requiresProject, reports);
