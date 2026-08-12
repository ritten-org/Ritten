using Hamelin.FileSystem;

namespace Wolfe.Hamelin.DotNet;

/// <summary>
/// Exposes functionality for interacting with .NET projects.
/// </summary>
public interface IDotNet
{
    /// <summary>
    /// Reads the package information from the given project file.
    /// </summary>
    Task<Project> ReadProject(IFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <c>dotnet restore</c>, throwing a <see cref="Commands.CommandFailedException"/> on failure.
    /// </summary>
    Task Restore(RestoreArgs args, CancellationToken cancellationToken = default);

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
    /// Reads the outcome of a test run from the given TRX results file.
    /// </summary>
    Task<TestRun> ReadTestResults(IFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts the compiler and MSBuild diagnostics from <c>dotnet build</c> output.
    /// </summary>
    IReadOnlyList<DotNetDiagnostic> ParseDiagnostics(string buildOutput);
}
