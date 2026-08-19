namespace Ritten.DotNet;

/// <summary>
/// The severity of a <see cref="DotNetDiagnostic"/>.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// A compiler or MSBuild warning.
    /// </summary>
    Warning,

    /// <summary>
    /// A compiler or MSBuild error.
    /// </summary>
    Error
}
