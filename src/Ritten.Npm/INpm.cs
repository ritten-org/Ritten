namespace Ritten.Npm;

/// <summary>
/// npm, in a project's directory.
/// </summary>
public interface INpm
{
    /// <summary>
    /// Installs exactly what the project's lock file says, with <c>npm ci</c>; throws when it cannot.
    /// </summary>
    Task Install(NpmProject project, CancellationToken ct = default);

    /// <summary>
    /// Runs one of the project's scripts, <c>npm run &lt;script&gt;</c>, and says how it went rather
    /// than throwing: a failing lint or test is an answer, not an error.
    /// </summary>
    Task<NpmScriptResult> Run(NpmProject project, string script, CancellationToken ct = default);
}
