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

    internal static bool IsDebug(Func<string, string?> envVar) => envVar(RunnerDebug) == "1";
}
