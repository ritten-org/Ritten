using Ritten.Contracts;

namespace Ritten.Reporting;

/// <summary>
/// Identifies a published report: the workflow it belongs to, and the subject of its run when it has one.
/// </summary>
public sealed record ReportKey
{
    /// <summary>
    /// Creates the key for a workflow's report, or for one subject of it.
    /// </summary>
    public ReportKey(string workflow, string? subject = null)
    {
        Workflow = Slug(workflow);
        Subject = subject is null ? null : Slug(subject);
    }

    /// <summary>
    /// The workflow, e.g. <c>ci</c>.
    /// </summary>
    public string Workflow { get; }

    /// <summary>
    /// The subject of the run, e.g. <c>caddy-infrastructure</c>.
    /// </summary>
    public string? Subject { get; }

    /// <summary>
    /// The key the given run's report is published under.
    /// </summary>
    public static ReportKey Of(RunContext context, WorkflowJob job) => new(context.Title, job.Subject);

    /// <summary>
    /// Reads a key written by <see cref="ToString"/>, or <c>null</c> when the value isn't one.
    /// </summary>
    public static ReportKey? Parse(string value) => value.Split(':', 2) switch
    {
        [{ Length: > 0 } workflow] => new ReportKey(workflow),
        [{ Length: > 0 } workflow, { Length: > 0 } subject] => new ReportKey(workflow, subject),
        _ => null
    };

    /// <inheritdoc />
    public override string ToString() => Subject is null ? Workflow : $"{Workflow}:{Subject}";

    private static string Slug(string value) =>
        string.Concat(value.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-'));
}
