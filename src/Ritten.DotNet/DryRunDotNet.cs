using NuGet.Versioning;
using Ritten.Contracts.FileSystem;
using Ritten.Engine;
using Ritten.Reporting;

namespace Ritten.DotNet;

/// <summary>
/// Reports what would be installed instead of installing it.
/// </summary>
internal class DryRunDotNet(IWorkflowLog log, IDotNet inner) : IDotNet
{
    /// <inheritdoc />
    public Task<Result<Project>> ReadProject(IFile file, CancellationToken cancellationToken = default) =>
        inner.ReadProject(file, cancellationToken);

    /// <inheritdoc />
    public Task<RestoreResult> Restore(RestoreArgs args, CancellationToken cancellationToken = default) =>
        inner.Restore(args, cancellationToken);

    /// <inheritdoc />
    public Task<BuildResult> Build(BuildArgs args, CancellationToken cancellationToken = default) =>
        inner.Build(args, cancellationToken);

    /// <inheritdoc />
    public Task<PackResult> Pack(PackArgs args, CancellationToken cancellationToken = default) =>
        inner.Pack(args, cancellationToken);

    /// <inheritdoc />
    public Task<TestResult> Test(TestArgs args, CancellationToken cancellationToken = default) =>
        inner.Test(args, cancellationToken);

    /// <inheritdoc />
    public Task<FormatResult> Format(FormatArgs args, CancellationToken cancellationToken = default) =>
        // Verifying changes nothing, so it passes through; formatting becomes verifying, which
        // names the same files without rewriting one. The step narrates which run it got.
        inner.Format(args with { VerifyNoChanges = true }, cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<string>>> SetVersion(SetVersionArgs args, CancellationToken cancellationToken = default)
    {
        log.Skipped($"Would set the version to {args.Version}.");
        return Task.FromResult(Result.Success<IReadOnlyList<string>>([]));
    }

    /// <inheritdoc />
    public Task<TestRun> ReadTestResults(IFile file, CancellationToken cancellationToken = default) =>
        inner.ReadTestResults(file, cancellationToken);

    /// <inheritdoc />
    public IReadOnlyList<DotNetDiagnostic> ParseDiagnostics(string buildOutput) =>
        inner.ParseDiagnostics(buildOutput);

    /// <inheritdoc />
    public Task<NuGetVersion?> InstalledToolVersion(string packageId, CancellationToken cancellationToken = default) =>
        inner.InstalledToolVersion(packageId, cancellationToken);

    /// <inheritdoc />
    public Task ToolInstall(ToolInstallArgs args, CancellationToken cancellationToken = default)
    {
        log.Skipped($"Would install {args.PackageId} {args.Version} globally from {args.Source.Name}.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ToolUninstall(string packageId, CancellationToken cancellationToken = default)
    {
        log.Skipped($"Would uninstall {packageId}.");
        return Task.CompletedTask;
    }
}
