using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Engine;
using Ritten.Engine.Workflows;
using Ritten.Workflows.Steps;

namespace Ritten.Workflows.DotNetTool;

/// <summary>
/// Builds, packs, and installs the tool globally from the working tree — no feed required.
/// </summary>
internal sealed class InstallJob : DotNetToolJob
{
    /// <inheritdoc />
    public override string Name => "install";

    /// <inheritdoc />
    public override string Description => "Builds, packs, and installs the tool globally from the working tree.";

    /// <inheritdoc />
    public override IReadOnlyList<JobArgument> Arguments { get; } = [ToolInputs.Reinstall];

    /// <inheritdoc />
    protected override void Configure(IWorkflowBuilder builder, DotNetToolSettings settings, JobArguments args)
    {
        base.Configure(builder, settings, args);
        builder.Services.AddSingleton(new ForceReinstall(args.IsSet(ToolInputs.Reinstall)));
    }

    /// <inheritdoc />
    protected override void ValidateSettings(SettingsValidator<DotNetToolSettings> settings) => settings
        .Require(s => s.Build.Project is not null || s.Build.Projects is { Count: > 0 }, "Set 'build.project' (one package) or 'build.projects' (several).")
        .Require(s => s.Build.Project is null || s.Build.Projects is null, "'build.project' and 'build.projects' are both set; use one.");

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
