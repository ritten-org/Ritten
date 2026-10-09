using Ritten.Contracts;
using Ritten.DotNet.Steps;
using Ritten.Engine.Workflows;
using Ritten.Workflows.Steps;

namespace Ritten.Workflows.DotNetTool;

/// <summary>
/// Builds, packs, and installs the tool globally from the working tree — no feed required.
/// </summary>
internal sealed class InstallJob : Job<InstallDotNetToolArguments>
{
    /// <inheritdoc />
    public override string Name => "install";

    /// <inheritdoc />
    public override string Description => "Builds, packs, and installs the tool globally from the working tree.";

    /// <inheritdoc />
    public override JobKind Kind => JobKind.Work;

    /// <inheritdoc />
    protected override void Validate(ArgumentsValidator<InstallDotNetToolArguments> arguments) => arguments
        .Require(a => a.Build.Project is not null || a.Build.Projects is { Count: > 0 }, "Set 'build.project' (one package) or 'build.projects' (several).")
        .Require(a => a.Build.Project is null || a.Build.Projects is null, "'build.project' and 'build.projects' are both set; use one.");

    /// <inheritdoc />
    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<Clean>(),
        Step.FromType<ReadProjects>(),
        Step.FromType<DotnetRestore>(),
        Step.FromType<DotnetBuild>(),
        Step.FromType<DotnetPack>(),
        Step.FromType<DotnetToolInstall>()
    ];
}
