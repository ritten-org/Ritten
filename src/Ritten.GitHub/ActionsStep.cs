namespace Ritten.GitHub;

/// <summary>
/// One step of a GitHub Actions job that runs a command, and where it runs it.
/// </summary>
/// <param name="Run">The script the step runs.</param>
/// <param name="WorkingDirectory">The directory the step runs in, when it sets one of its own.</param>
public sealed record ActionsStep(string Run, string? WorkingDirectory);
