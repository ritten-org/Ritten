using Ritten.Contracts.FileSystem;

namespace Ritten.DotNet;

/// <summary>
/// What a repository holds, read from the files themselves rather than from any configuration.
/// </summary>
public static class DotNetProjects
{
    /// <summary>
    /// The shared build properties every project in a repository inherits.
    /// </summary>
    private const string SharedProperties = "Directory.Build.props";

    /// <summary>
    /// The directory a repository keeps its tool manifest in, by dotnet's convention.
    /// </summary>
    public const string ToolManifestDirectory = ".config";

    /// <summary>
    /// The two names the SDK will read a directory's tool manifest under. The conventional one
    /// is first, which is also the one a new manifest is written to.
    /// </summary>
    private static readonly string[] ToolManifestNames = [$"{ToolManifestDirectory}/dotnet-tools.json", "dotnet-tools.json"];

    /// <summary>
    /// The repository's tool manifest, wherever the SDK would read it from, or null when the
    /// repository has none of its own.
    /// </summary>
    /// <param name="root">The directory the manifest belongs in.</param>
    public static IFile? ToolManifest(IDirectory root) =>
        ToolManifestNames.Select(root.GetFile).FirstOrDefault(file => file.Exists);

    /// <summary>
    /// Every project in the repository, in path order and without the build output's copies.
    /// </summary>
    /// <param name="root">The directory to look under.</param>
    public static IEnumerable<IFile> Projects(IDirectory root) => root
        .GetFiles("**/*.csproj")
        .Where(file => !IsBuildOutput(file))
        .OrderBy(file => file.AbsolutePath, StringComparer.Ordinal);

    /// <summary>
    /// Whether the project is a test project, by two conventions: what it's called, and where it lives.
    /// </summary>
    /// <param name="project">The project to judge.</param>
    public static bool IsTests(IFile project) =>
        project.NameWithoutExtension.EndsWith("Tests", StringComparison.OrdinalIgnoreCase)
        || Segments(project).Contains("tests", StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The first file in the repository that declares the given MSBuild property.
    /// </summary>
    /// <param name="root">The directory to look under.</param>
    /// <param name="element">The literal element to look for, e.g. <c>&lt;PackAsTool&gt;true&lt;/PackAsTool&gt;</c>.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public static async Task<IFile?> FileContainingMsBuildElement(IDirectory root, string element, CancellationToken cancellationToken = default)
    {
        var shared = root.GetFile(SharedProperties);
        foreach (var file in shared.Exists ? [shared, .. Projects(root)] : Projects(root))
        {
            if (await Declares(file, element, cancellationToken))
            {
                return file;
            }
        }

        return null;
    }

    /// <summary>
    /// Whether the given file declares the given MSBuild property.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <param name="element">The literal element to look for.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public static async Task<bool> Declares(IFile file, string element, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(file.OpenRead());
        var content = await reader.ReadToEndAsync(cancellationToken);
        return content.Contains(element, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBuildOutput(IFile file) =>
        Segments(file).Any(segment => segment is "bin" or "obj");

    private static string[] Segments(IFile file) =>
        file.AbsolutePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
