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
    /// Reads the outcome of a test run from the given TRX results file.
    /// </summary>
    Task<TestRun> ReadTestResults(IFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts the compiler and MSBuild diagnostics from <c>dotnet build</c> output.
    /// </summary>
    IReadOnlyList<DotNetDiagnostic> ParseDiagnostics(string buildOutput);
}
