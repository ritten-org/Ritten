using Ritten.Changelogs;

namespace Ritten.Workflows.DotNet;

/// <summary>
/// What setting a repository up for the plain .NET workflow runs with: the project file's arguments, and the
/// changelog it starts.
/// </summary>
public sealed record DotNetInitArguments : DotNetArguments
{
    /// <summary>
    /// The changelog to start, or top up.
    /// </summary>
    public ChangelogSettings Changelog { get; init; } = new();
}