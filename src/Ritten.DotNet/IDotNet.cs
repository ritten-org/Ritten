using NuGet.Versioning;
using Ritten.Contracts.FileSystem;
using Ritten.Engine;

namespace Ritten.DotNet;

/// <summary>
/// Exposes functionality for interacting with .NET projects.
/// </summary>
public interface IDotNet
{
    /// <summary>
    /// Reads the package information from the given project file.
    /// </summary>
    Task<Result<Project>> ReadProject(IFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <c>dotnet restore</c> and returns the outcome.
    /// </summary>
    Task<RestoreResult> Restore(RestoreArgs args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <c>dotnet build</c> and returns the outcome, with any compiler output.
    /// </summary>
    Task<BuildResult> Build(BuildArgs args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <c>dotnet pack</c> and returns the packages it produced, throwing a
    /// <see cref="Commands.CommandFailedException"/> on failure.
    /// </summary>
    Task<PackResult> Pack(PackArgs args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <c>dotnet test</c> with a TRX logger and returns the outcome.
    /// </summary>
    Task<TestResult> Test(TestArgs args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <c>dotnet format --verify-no-changes</c> and returns the outcome.
    /// </summary>
    Task<FormatResult> CheckFormat(FormatArgs args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <c>dotnet format</c>, fixing what it can, and reports which files it changed.
    /// </summary>
    Task<FormatResult> Format(FormatArgs args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the current project version.
    /// </summary>
    /// <remarks>
    /// This works by writing a new version into every file that literally declares the current one,
    /// then returns those files. Fails when the current version isn't found.
    /// </remarks>
    Task<Result<IReadOnlyList<string>>> SetVersion(SetVersionArgs args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the outcome of a test run from the given TRX results file.
    /// </summary>
    Task<TestRun> ReadTestResults(IFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the version of the given tool installed globally, or null when it isn't installed.
    /// </summary>
    Task<NuGetVersion?> InstalledToolVersion(string packageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <c>dotnet tool install --global</c> against the given source alone, throwing a <see cref="Commands.CommandFailedException"/> on failure.
    /// </summary>
    Task ToolInstall(ToolInstallArgs args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <c>dotnet tool uninstall --global</c>, throwing a <see cref="Commands.CommandFailedException"/> on failure.
    /// </summary>
    Task ToolUninstall(string packageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts the compiler and MSBuild diagnostics from <c>dotnet build</c> output.
    /// </summary>
    IReadOnlyList<DotNetDiagnostic> ParseDiagnostics(string buildOutput);
}
