using Ritten.Contracts.FileSystem;

namespace Ritten.Init;

/// <summary>
/// Puts a repository's scaffolding in place, or reports how far it has drifted from what the
/// workflow expects.
/// </summary>
/// <param name="fileSystem">The file system.</param>
public sealed class Scaffolder(IFileSystem fileSystem)
{
    /// <summary>
    /// Puts the scaffolding in place as far as the mode allows, and reports what became of each file.
    /// </summary>
    /// <param name="files">What the repository should have.</param>
    /// <param name="root">The directory the files belong under.</param>
    /// <param name="mode">How far to go.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<IReadOnlyList<(ScaffoldedFile File, ScaffoldOutcome Outcome)>> Apply(
        IReadOnlyList<ScaffoldedFile> files,
        IDirectory? root = null,
        ScaffoldMode mode = ScaffoldMode.Write,
        CancellationToken cancellationToken = default)
    {
        root ??= fileSystem.ProjectRoot;
        List<(ScaffoldedFile, ScaffoldOutcome)> outcomes = [];
        foreach (var file in files)
        {
            var target = root.GetFile(file.Path);
            if (target.Exists)
            {
                // A seed is the repository's the moment it exists; only what Ritten generates
                // is held to what Ritten would generate.
                if (!file.Generated || await Read(target, cancellationToken) == file.Content)
                {
                    outcomes.Add((file, ScaffoldOutcome.Matches));
                    continue;
                }

                if (mode != ScaffoldMode.Rewrite)
                {
                    outcomes.Add((file, ScaffoldOutcome.Differs));
                    continue;
                }

                await Write(root, file, cancellationToken);
                outcomes.Add((file, ScaffoldOutcome.Rewritten));
                continue;
            }

            if (mode != ScaffoldMode.Check)
            {
                await Write(root, file, cancellationToken);
            }

            outcomes.Add((file, ScaffoldOutcome.Written));
        }

        return outcomes;
    }

    private static async Task<string> Read(IFile file, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(file.OpenRead());
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static async Task Write(IDirectory root, ScaffoldedFile file, CancellationToken cancellationToken)
    {
        // .config and .github/workflows won't exist yet in a fresh repository.
        if (Path.GetDirectoryName(file.Path) is { Length: > 0 } directory)
        {
            root.GetDirectory(directory).Create();
        }

        var stream = root.GetFile(file.Path).OpenWrite();
        stream.SetLength(0);
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(file.Content.AsMemory(), cancellationToken);
    }
}
