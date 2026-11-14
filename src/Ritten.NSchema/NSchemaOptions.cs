namespace Ritten.NSchema;

/// <summary>
/// How the client runs NSchema: the client's own setting, not a project's.
/// </summary>
public sealed class NSchemaOptions
{
    /// <summary>
    /// The <c>nschema</c> executable to run. When left <c>null</c>,
    /// it runs <c>dotnet tool restore</c>, then <c>dotnet nschema</c>.
    /// </summary>
    public string? Executable { get; set; }

    /// <summary>
    /// Uses only the plugins already in NSchema's cache (<c>--no-init</c>.
    /// </summary>
    public bool NoInit { get; set; }
}
