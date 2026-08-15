namespace Ritten.DotNet;

/// <summary>
/// The outcome of a <c>dotnet restore</c>.
/// </summary>
public record RestoreResult
{
    /// <summary>
    /// The projects the restore actually touched; empty when everything was already up to date.
    /// </summary>
    public IReadOnlyList<string> RestoredProjects { get; init; } = [];
}
