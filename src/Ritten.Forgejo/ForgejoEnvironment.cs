namespace Ritten.Forgejo;

/// <summary>
/// What the Forgejo runner exports.
/// </summary>
/// <remarks>
/// Every <c>FORGEJO_*</c> variable is mirrored as a
/// <c>GITHUB_*</c> one for workflows written against GitHub, so the mirror is what an older
/// runner offers when the native name is absent.
/// </remarks>
internal static class ForgejoEnvironment
{
    public const string Actions = "FORGEJO_ACTIONS";
    public const string GitHubActions = "GITHUB_ACTIONS";
    public const string ServerUrl = "FORGEJO_SERVER_URL";
    public const string Repository = "FORGEJO_REPOSITORY";
    public const string RunNumber = "FORGEJO_RUN_NUMBER";
    public const string RunId = "FORGEJO_RUN_ID";
    public const string Workflow = "FORGEJO_WORKFLOW";

    /// <summary>
    /// The ref being built; pull request runs use <c>refs/pull/&lt;number&gt;/head</c>.
    /// </summary>
    public const string Ref = "FORGEJO_REF";

    /// <summary>
    /// The ref a pull request wants to merge into; set only on pull request runs.
    /// </summary>
    public const string BaseRef = "FORGEJO_BASE_REF";

    /// <summary>
    /// The run's own API token, which the runner exports to every step.
    /// </summary>
    public const string Token = "FORGEJO_TOKEN";

    /// <summary>
    /// The file holding the event that triggered the run, as Forgejo sent it.
    /// </summary>
    public const string EventPath = "FORGEJO_EVENT_PATH";
    public const string StepSummary = "GITHUB_STEP_SUMMARY";
    public const string RunnerDebug = "RUNNER_DEBUG";

    /// <summary>
    /// The <c>GITHUB_*</c> spelling of a <c>FORGEJO_*</c> name.
    /// </summary>
    internal static string Mirror(string name) => "GITHUB_" + name["FORGEJO_".Length..];

    /// <summary>
    /// Reads a variable by its native name, else by its mirror.
    /// </summary>
    internal static string? Read(Func<string, string?> envVar, string name) =>
        envVar(name) is { Length: > 0 } native ? native : envVar(Mirror(name));

    /// <summary>
    /// A file's contents, or <c>null</c> when there is no such file.
    /// </summary>
    internal static string? ReadFile(string path) => File.Exists(path) ? File.ReadAllText(path) : null;

    internal static bool IsDebug(Func<string, string?> envVar) => envVar(RunnerDebug) == "1";
}
