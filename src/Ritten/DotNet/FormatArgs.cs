using Ritten.Contracts.FileSystem;

namespace Ritten.DotNet;

/// <summary>
/// Settings for a <see cref="IDotNet.CheckFormat"/> invocation.
/// </summary>
public record FormatArgs
{
    /// <summary>
    /// The directory the <c>dotnet format</c> report is written to and read back from;
    /// created if it doesn't exist.
    /// </summary>
    public required IDirectory ReportDirectory { get; init; }

    /// <summary>
    /// Whether to disable restoring packages on format.
    /// </summary>
    public bool NoRestore { get; init; }
}
