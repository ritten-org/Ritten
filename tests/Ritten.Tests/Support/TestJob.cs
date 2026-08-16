using Ritten.Core;
using Ritten.Pipelines;

namespace Ritten.Tests.Support;

/// <summary>
/// A job declared inline: the steps and validation a test hands it, nothing more.
/// </summary>
internal sealed class TestJob(
    string name = "verify",
    IReadOnlyList<Type>? steps = null,
    Action<SettingsValidator<DotNetToolSettings>>? validate = null
) : Job<DotNetToolSettings>
{
    public override string Name => name;

    protected override IEnumerable<Type> StepTypes => steps ?? [];

    protected override void ValidateSettings(SettingsValidator<DotNetToolSettings> settings) => validate?.Invoke(settings);
}
