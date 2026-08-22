using Ritten.Contracts.FileSystem;

namespace Ritten.DotNet;

/// <summary>
/// Where a tool command applies: the machine's own tools, or the manifest governing a directory.
/// </summary>
public sealed record ToolScope
{
    private ToolScope(IDirectory? directory) => Directory = directory;

    /// <summary>
    /// The machine's tools, installed for the user.
    /// </summary>
    public static ToolScope Global { get; } = new((IDirectory?)null);

    /// <summary>
    /// The tools pinned by the manifest governing the given directory.
    /// </summary>
    /// <param name="directory">The directory the manifest is resolved from.</param>
    public static ToolScope Local(IDirectory directory) => new(directory);

    /// <summary>
    /// The directory a local command runs in, or null for the machine's own tools.
    /// </summary>
    public IDirectory? Directory { get; }

    /// <summary>
    /// Whether this is the machine's own tools rather than a repository's.
    /// </summary>
    public bool IsGlobal => Directory is null;

    /// <summary>
    /// The flag the SDK spells this scope with.
    /// </summary>
    internal string Flag => IsGlobal ? "--global" : "--local";
}
