using Ritten.Contracts.FileSystem;

namespace Ritten.NSchema;

/// <summary>
/// What a command runs under: the project's environment, and the variables that reach NSchema.
/// </summary>
/// <remarks>
/// NSchema reads any <c>DATABASE</c> or <c>STATE</c> setting from <c>NSCHEMA_&lt;KEYWORD&gt;_&lt;SETTING&gt;</c>
/// (<c>NSCHEMA_DATABASE_CONNECTION_STRING</c>, <c>NSCHEMA_STATE_PATH</c>), so this is how a caller
/// points one project at its database and state without editing the project's files.
/// </remarks>
public sealed record NSchemaEnvironment
{
    /// <summary>
    /// The project's environment (<c>--environment</c>): its <c>*.env.&lt;name&gt;.sql</c> files laid
    /// over the rest. None for the project's base configuration alone.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The env files every command runs under, in order; a later file's entry replaces an earlier one's.
    /// </summary>
    public IReadOnlyList<IFile> EnvFiles { get; init; } = [];

    /// <summary>
    /// Variables every command runs under, over the env files'.
    /// </summary>
    public IReadOnlyDictionary<string, string> Variables { get; init; } = new Dictionary<string, string>();
}
