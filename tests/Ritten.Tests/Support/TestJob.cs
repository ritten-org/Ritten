using Ritten.Contracts;
using Ritten.Engine.Workflows;
using Ritten.Workflows;

namespace Ritten.Tests.Support;

/// <summary>
/// A job declared inline: the steps and checks a test hands it, nothing more.
/// </summary>
internal sealed class TestJob(
    string name = "verify",
    IReadOnlyList<Step>? steps = null,
    Action<SettingsValidator<DotNetToolSettings>>? validate = null
) : Job<DotNetToolSettings>
{
    public override string Name => name;

    public override IReadOnlyList<Step> Steps { get; } = steps ?? [];

    protected override void ValidateSettings(SettingsValidator<DotNetToolSettings> settings) => validate?.Invoke(settings);
}
