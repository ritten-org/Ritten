using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Ritten.Forgejo;

/// <summary>
/// The facts of the Forgejo Actions run a job is part of.
/// </summary>
public class ForgejoActionsOptions
{
    /// <summary>
    /// The Forgejo instance running the workflow.
    /// </summary>
    /// <remarks>
    /// The runner reports the address it connected with, not the instance's public one.
    /// </remarks>
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
    /// The number of the pull request that triggered the run, if there is one.
    /// </summary>
    public int? PullRequestNumber { get; set; }

    /// <summary>
    /// The ref the pull request wants to merge into, if the run is one.
    /// </summary>
    public string? BaseRef { get; set; }

    /// <summary>
    /// The token the run authenticates to the API with, or <c>null</c> when the runner did not
    /// export one.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// The instance's API root, or <c>null</c> when the runner did not say where the instance is.
    /// </summary>
    public string? ApiUrl { get; set; }

    /// <summary>
    /// True if the run was triggered by a pull request, otherwise false.
    /// </summary>
    [MemberNotNullWhen(true, nameof(PullRequestNumber))]
    public bool IsPullRequest => PullRequestNumber != null;

    /// <summary>
    /// The file the job summary is appended to, when the runner offers one.
    /// </summary>
    public string? SummaryFile { get; set; }

    /// <summary>
    /// The run's page, or <c>null</c> when the runner said too little to build it.
    /// </summary>
    public string? RunUrl { get; set; }

    internal static void ConfigureFromEnvironment(ForgejoActionsOptions options, Func<string, string?> envVar, Func<string, string?> readFile)
    {
        options.ServerUrl = ForgejoEnvironment.Read(envVar, ForgejoEnvironment.ServerUrl)?.TrimEnd('/');
        options.Repository = ForgejoEnvironment.Read(envVar, ForgejoEnvironment.Repository);
        options.RunNumber = Parse(ForgejoEnvironment.Read(envVar, ForgejoEnvironment.RunNumber));
        options.RunId = Parse(ForgejoEnvironment.Read(envVar, ForgejoEnvironment.RunId));
        options.Workflow = ForgejoEnvironment.Read(envVar, ForgejoEnvironment.Workflow);
        options.PullRequestNumber = ParsePullRequestNumber(ForgejoEnvironment.Read(envVar, ForgejoEnvironment.Ref));
        options.BaseRef = ForgejoEnvironment.Read(envVar, ForgejoEnvironment.BaseRef);
        options.Token = ForgejoEnvironment.Read(envVar, ForgejoEnvironment.Token);
        options.SummaryFile = envVar(ForgejoEnvironment.StepSummary);
        options.ApiUrl = options.ServerUrl is null ? null : $"{options.ServerUrl}/api/v1/";
        var repositoryUrl = ReadRepositoryUrl(ForgejoEnvironment.Read(envVar, ForgejoEnvironment.EventPath), readFile)
            ?? (options.ServerUrl is null || options.Repository is null ? null : $"{options.ServerUrl}/{options.Repository}");
        options.RunUrl = repositoryUrl is null || options.RunNumber is null
            ? null
            : $"{repositoryUrl}/actions/runs/{options.RunNumber}";
    }

    /// <summary>
    /// The repository's public address, from the event that triggered the run.
    /// </summary>
    private static string? ReadRepositoryUrl(string? eventPath, Func<string, string?> readFile)
    {
        if (eventPath is null || readFile(eventPath) is not { } json)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("repository", out var repository)
                && repository.ValueKind == JsonValueKind.Object
                && repository.TryGetProperty("html_url", out var url)
                && url.ValueKind == JsonValueKind.String
                && url.GetString() is { Length: > 0 } address
                    ? address.TrimEnd('/')
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static long? Parse(string? value) => long.TryParse(value, out var number) ? number : null;

    private static int? ParsePullRequestNumber(string? reference)
    {
        // Pull request runs check out a ref of the form `refs/pull/<number>/head`.
        if (reference?.StartsWith("refs/pull/") != true)
        {
            return null;
        }

        return int.TryParse(reference.Split('/')[2], out var pullRequestNumber) ? pullRequestNumber : null;
    }
}
