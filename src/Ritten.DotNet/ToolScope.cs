using Ritten.Contracts.FileSystem;

namespace Ritten.DotNet;

/// <summary>
/// Which install location to fetch the tool from.
/// </summary>
public sealed record ToolScope
{
    private readonly Kind _kind;

    private ToolScope(Kind kind, IDirectory? directory)
    {
        _kind = kind;
        Directory = directory;
    }

    private enum Kind
    {
        Global,
        Local,
        Path
    }

    /// <summary>
    /// The machine's tools, installed for the user.
    /// </summary>
    public static ToolScope Global { get; } = new(Kind.Global, null);

    /// <summary>
    /// The tools pinned by the manifest governing the given directory.
    /// </summary>
    /// <param name="directory">The directory the manifest is resolved from.</param>
    public static ToolScope Local(IDirectory directory) => new(Kind.Local, directory);

    /// <summary>
    /// The tools installed into the given directory, each command at its top: an install of its
    /// own, run by its full path from anywhere, that neither the user's tools nor a manifest share.
    /// </summary>
    /// <param name="directory">The directory the tools are installed into.</param>
    public static ToolScope Path(IDirectory directory) => new(Kind.Path, directory);

    /// <summary>
    /// The directory a local scope resolves its manifest from, or a path scope installs into; null
    /// for the machine's own tools.
    /// </summary>
    public IDirectory? Directory { get; }

    /// <summary>
    /// Whether this is the machine's own tools.
    /// </summary>
    public bool IsGlobal => _kind == Kind.Global;

    /// <summary>
    /// Whether this is a directory the tools are installed into.
    /// </summary>
    public bool IsPath => _kind == Kind.Path;

    /// <summary>
    /// The flags the SDK spells this scope with.
    /// </summary>
    internal IReadOnlyList<string> Flags => (_kind, Directory) switch
    {
        (Kind.Local, _) => ["--local"],
        (Kind.Path, { } directory) => ["--tool-path", directory.AbsolutePath],
        _ => ["--global"]
    };

    /// <summary>
    /// Where a command in this scope runs: a local one where its manifest is resolved from; the
    /// others anywhere.
    /// </summary>
    internal IDirectory? WorkingDirectory => _kind == Kind.Local ? Directory : null;
}
