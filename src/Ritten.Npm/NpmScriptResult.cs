namespace Ritten.Npm;

/// <summary>
/// How one of the project's scripts went.
/// </summary>
/// <param name="Succeeded">Whether it exited cleanly.</param>
/// <param name="Output">What it wrote, standard output then standard error.</param>
public sealed record NpmScriptResult(bool Succeeded, string Output);
