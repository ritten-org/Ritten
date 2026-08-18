namespace Ritten.Engine;

/// <summary>
/// The process exit codes a workflow run can produce.
/// </summary>
public static class WorkflowExitCodes
{
    /// <summary>
    /// Indicates every step completed successfully.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// Indicates a step failed.
    /// </summary>
    public const int Failed = 1;

    /// <summary>
    /// Indicates the workflow never started because its configuration is invalid.
    /// </summary>
    public const int ConfigurationError = 2;

    /// <summary>
    /// Indicates the run was cancelled. Follows the shell convention of 128 + SIGINT.
    /// </summary>
    public const int Cancelled = 130;
}
