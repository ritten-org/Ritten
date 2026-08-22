using Ritten.Changelogs;
using Ritten.Contracts;
using Ritten.DotNet.Steps;
using Ritten.Engine;
using Ritten.Engine.Workflows;
using Ritten.Git;
using Ritten.GitHub;
using Ritten.Init;
using Ritten.Init.Steps;

namespace Ritten.Workflows.DotNet;

/// <summary>
/// Sets a repository up to run this workflow, and brings one already set up back up to date.
/// </summary>
internal sealed class InitJob : Job<DotNetSettings>
{
    /// <inheritdoc />
    public override string Name => "init";

    /// <inheritdoc />
    public override string Description => "Sets this repository up to run the workflow, and tops up whatever it's missing.";

    /// <inheritdoc />
    public override JobKind Kind => JobKind.Work;

    /// <inheritdoc />
    public override bool RequiresProject => false;

    /// <inheritdoc />
    protected override void Configure(IWorkflowBuilder builder, DotNetSettings settings) => builder
        .AddChangelogs(new ChangelogSettings())
        .AddDotNet(settings.Build)
        .AddGit()
        .AddGitHubActions()
        .AddInit(RittenTool.Pin);

    /// <inheritdoc />
    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<FindProjects>(),
        Step.FromType<EnsureRittenProject>(),
        Step.FromType<EnsureChangelog>(),
        Step.FromType<EnsureToolManifest>(),
        Step.FromType<EnsureActionsWorkflow>()
    ];
}
