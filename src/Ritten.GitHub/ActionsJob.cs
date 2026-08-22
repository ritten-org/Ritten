namespace Ritten.GitHub;

/// <summary>
/// One job of a GitHub Actions workflow: as much of it as anything needs to recognise its own.
/// </summary>
/// <param name="Id">The job's id.</param>
/// <param name="Steps">The job's steps that run a command, in order.</param>
public sealed record ActionsJob(string Id, IReadOnlyList<ActionsStep> Steps)
{
    /// <summary>
    /// Whether any of the job's steps run the given command.
    /// </summary>
    /// <param name="command">The command to look for, e.g. <c>dotnet ritten check</c>.</param>
    public bool Invokes(string command) => Steps.Any(step => step.Run.Contains(command, StringComparison.Ordinal));
}
