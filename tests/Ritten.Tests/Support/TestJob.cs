using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Engine.Workflows;
using Ritten.Workflows;

namespace Ritten.Tests.Support;

/// <summary>
/// A job declared inline: the steps and checks a test hands it, nothing more.
/// </summary>
internal sealed class TestJob(
    string name = "verify",
    IReadOnlyList<Step>? steps = null,
    Action<SettingsValidator<DotNetToolSettings>>? validate = null,
    IReadOnlyList<JobArgument>? arguments = null,
    JobKind kind = JobKind.Work,
    Action<IWorkflowBuilder, JobArguments>? configure = null
) : Job<DotNetToolSettings>
{
    public override string Name => name;

    public override string Description => $"The {name} job, declared by a test.";

    public override JobKind Kind => kind;

    public override IReadOnlyList<Step> Steps { get; } = steps ?? [];

    public override IReadOnlyList<JobArgument> Arguments { get; } = arguments ?? [];

    protected override void ValidateSettings(SettingsValidator<DotNetToolSettings> settings) => validate?.Invoke(settings);

    protected override void Configure(IWorkflowBuilder builder, DotNetToolSettings settings, JobArguments args) =>
        configure?.Invoke(builder, args);
}
