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
    /// Runs <c>dotnet format</c> and reports which files weren't formatted.
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
    /// Runs <c>dotnet tool list</c> and reads the version of the given tool, or null when it isn't installed.
    /// </summary>
    /// <param name="packageId">The package ID of the tool to look for.</param>
    /// <param name="scope">Whether to ask the machine or a repository's manifest.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<NuGetVersion?> InstalledToolVersion(string packageId, ToolScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <c>dotnet tool install</c>.
    /// </summary>
    /// <param name="args">What to install, and where.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task ToolInstall(ToolInstallArgs args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <c>dotnet tool update</c>.
    /// </summary>
    /// <param name="args">What to move, and where.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task ToolUpdate(ToolInstallArgs args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <c>dotnet tool uninstall</c>.
    /// </summary>
    /// <param name="packageId">The package ID of the tool to remove.</param>
    /// <param name="scope">Whether to remove it from the machine or from a repository's manifest.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task ToolUninstall(string packageId, ToolScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <c>dotnet new tool-manifest</c> in the given directory.
    /// </summary>
    /// <param name="directory">The directory to create the manifest in.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task CreateToolManifest(IDirectory directory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts the compiler and MSBuild diagnostics from <c>dotnet build</c> output.
    /// </summary>
    IReadOnlyList<DotNetDiagnostic> ParseDiagnostics(string buildOutput);
}
