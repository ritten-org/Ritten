namespace Ritten.NSchema;

/// <summary>
/// Provides an interface for running the NSchema CLI.
/// </summary>
public interface INSchema
{
    /// <summary>
    /// Makes sure the files are formatted correctly, and returns any that are formatted badly.
    /// </summary>
    /// <exception cref="Commands.CommandFailedException">A file is not valid NSchema, or the project could not be read.</exception>
    Task<IReadOnlyList<string>?> VerifyFormatting(NSchemaProject project, CancellationToken ct = default);

    /// <summary>
    /// Gets a migration plan for the given project.
    /// </summary>
    /// <exception cref="Commands.CommandFailedException">The plan could not be made.</exception>
    Task<NSchemaPlanResult> Plan(NSchemaProject project, NSchemaEnvironment? environment = null, CancellationToken ct = default);

    /// <summary>
    /// Applies the migration to the current project.
    /// </summary>
    /// <exception cref="Commands.CommandFailedException">The apply failed, or was refused.</exception>
    Task<NSchemaApplyResult> Apply(NSchemaProject project, NSchemaEnvironment? environment = null, NSchemaApplySettings? settings = null, CancellationToken ct = default);
}
