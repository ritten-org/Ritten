namespace Ritten.Init;

/// <summary>
/// A file a repository needs in order to run its workflow, and what it should contain.
/// </summary>
/// <param name="Path">Where the file belongs, relative to the repository root.</param>
/// <param name="Content">What the file should say.</param>
/// <param name="Generated">Whether Ritten owns the file's content. A generated file can be checked for drift.</param>
public sealed record ScaffoldedFile(string Path, string Content, bool Generated = false);
