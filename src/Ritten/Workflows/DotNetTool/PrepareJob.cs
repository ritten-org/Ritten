using Ritten.Changelogs.Steps;
using Ritten.Contracts;
using Ritten.DotNet.Steps;
using Ritten.Engine.Workflows;
using Ritten.NuGet.Steps;
using Ritten.Releases.Steps;

namespace Ritten.Workflows.DotNetTool;

/// <summary>
/// Stages the repository for its next release, fixing what it can of whatever stands in the way.
/// </summary>
internal sealed class PrepareJob : Job<PrepareDotNetToolArguments>
{
    /// <inheritdoc />
    public override string Name => "prepare";

    /// <inheritdoc />
    public override string Description => "Stages the next release: rolls the changelog, sets the version, and formats.";

    /// <inheritdoc />
    public override JobKind Kind => JobKind.Work;

    /// <inheritdoc />
    protected override void Validate(ArgumentsValidator<PrepareDotNetToolArguments> arguments) => arguments
        .Require(a => a.Build.Project is not null || a.Build.Projects is { Count: > 0 }, "Set 'build.project' (one package) or 'build.projects' (several).")
        .Require(a => a.Build.Project is null || a.Build.Projects is null, "'build.project' and 'build.projects' are both set; use one.");

    /// <inheritdoc />
    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ReadProjects>(),
        Step.FromType<ResolveRelease>(),
        Step.FromType<ReadChangelog>(),
        Step.FromType<NugetRead>(),
        Step.FromType<DecideVersion>(),
        Step.FromType<PrepareChangelog>(),
        Step.FromType<PrepareVersion>(),
        Step.FromType<DotnetFormat>()
    ];
}
