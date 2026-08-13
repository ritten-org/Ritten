namespace Ritten.DotNet;

/// <summary>
/// A compiler or MSBuild diagnostic parsed from <c>dotnet build</c> output.
/// </summary>
public record DotNetDiagnostic
{
    /// <summary>
    /// The severity of the diagnostic.
    /// </summary>
    public required DiagnosticSeverity Severity { get; init; }

    /// <summary>
    /// The diagnostic code, e.g. <c>CS0103</c> or <c>NU1101</c>.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// The diagnostic message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The source file the diagnostic points at, if it has one.
    /// </summary>
    public string? File { get; init; }

    /// <summary>
    /// The line within <see cref="File"/>, if the diagnostic has a location.
    /// </summary>
    public int? Line { get; init; }

    /// <summary>
    /// The column within <see cref="File"/>, if the diagnostic has a location.
    /// </summary>
    public int? Column { get; init; }

    /// <summary>
    /// Renders the diagnostic in the familiar MSBuild format, e.g.
    /// <c>Program.cs(12,34): error CS0103: The name 'x' does not exist</c>.
    /// </summary>
    public override string ToString()
    {
        var severity = Severity == DiagnosticSeverity.Error ? "error" : "warning";
        var location = File is null ? "" : $"{File}({Line},{Column}): ";
        return $"{location}{severity} {Code}: {Message}";
    }
}
