namespace Ritten.DotNet;

/// <summary>
/// The outcome of a <c>dotnet restore</c>.
/// </summary>
public record RestoreResult
{
    /// <summary>
    /// True if the restore succeeded, otherwise false.
    /// </summary>
    public required bool Succeeded { get; init; }

    /// <summary>
    /// The projects the restore actually touched; empty when everything was already up to date.
    /// </summary>
    public IReadOnlyList<string> RestoredProjects { get; init; } = [];

    /// <summary>
    /// The NuGet and MSBuild diagnostics extracted from the restore output,
    /// with duplicates across projects collapsed.
    /// </summary>
    public IReadOnlyList<DotNetDiagnostic> Diagnostics { get; init; } = [];
}
