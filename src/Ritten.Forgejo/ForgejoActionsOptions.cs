namespace Ritten.Forgejo;

/// <summary>
/// The facts of the Forgejo Actions run a job is part of.
/// </summary>
public class ForgejoActionsOptions
{
    /// <summary>
    /// The Forgejo instance running the workflow.
    /// </summary>
    public string? ServerUrl { get; set; }

    /// <summary>
    /// The repository, as <c>owner/name</c>.
    /// </summary>
    public string? Repository { get; set; }

    /// <summary>
    /// The run's number within the repository, which is what its page is addressed by.
    /// </summary>
    public long? RunNumber { get; set; }

    /// <summary>
    /// The run's id across the instance.
    /// </summary>
    public long? RunId { get; set; }

    /// <summary>
    /// The workflow's name.
    /// </summary>
    public string? Workflow { get; set; }

    /// <summary>
    /// The file the job summary is appended to, when the runner offers one.
    /// </summary>
    public string? SummaryFile { get; set; }

    /// <summary>
    /// The run's page, or <c>null</c> when the runner said too little to build it.
    /// </summary>
    public string? RunUrl { get; set; }

    internal static void ConfigureFromEnvironment(ForgejoActionsOptions options, Func<string, string?> envVar)
    {
        options.ServerUrl = ForgejoEnvironment.Read(envVar, ForgejoEnvironment.ServerUrl)?.TrimEnd('/');
        options.Repository = ForgejoEnvironment.Read(envVar, ForgejoEnvironment.Repository);
        options.RunNumber = Parse(ForgejoEnvironment.Read(envVar, ForgejoEnvironment.RunNumber));
        options.RunId = Parse(ForgejoEnvironment.Read(envVar, ForgejoEnvironment.RunId));
        options.Workflow = ForgejoEnvironment.Read(envVar, ForgejoEnvironment.Workflow);
        options.SummaryFile = envVar(ForgejoEnvironment.StepSummary);
        options.RunUrl = options.ServerUrl is null || options.Repository is null || options.RunNumber is null
            ? null
            : $"{options.ServerUrl}/{options.Repository}/actions/runs/{options.RunNumber}";
    }

    private static long? Parse(string? value) => long.TryParse(value, out var number) ? number : null;
}
