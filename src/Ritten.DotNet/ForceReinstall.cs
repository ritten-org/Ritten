namespace Ritten.DotNet;

/// <summary>
/// Whether an install should replace a tool that already carries the version being installed.
/// </summary>
/// <param name="Requested">Whether the caller asked for the reinstall.</param>
public sealed record ForceReinstall(bool Requested);
