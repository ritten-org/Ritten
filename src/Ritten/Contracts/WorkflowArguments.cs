namespace Ritten.Contracts;

/// <summary>
/// The command-line arguments Ritten understands.
/// </summary>
public static class WorkflowArguments
{
    /// <summary>
    /// Approves a job up front, for runs with nobody there to ask.
    /// </summary>
    public const string AutoApprove = "auto-approve";

    /// <summary>
    /// Rehearses a job without doing anything that reaches outside the project.
    /// </summary>
    public const string DryRun = "dry-run";

    /// <summary>
    /// Shows every log entry in its highest detail.
    /// </summary>
    public const string Verbose = "verbose";

    /// <summary>
    /// Shows only failures.
    /// </summary>
    public const string Quiet = "quiet";
}
