namespace Ritten.Contracts.FileSystem;

/// <summary>
/// Contains extension methods for <see cref="IDirectory"/>.
/// </summary>
public static class DirectoryExtensions
{
    extension(IDirectory directory)
    {
        /// <summary>
        /// The path of the given file relative to this directory.
        /// </summary>
        /// <param name="file">The file to write the path of.</param>
        public string RelativePath(IFile file) => Relative(directory, file.AbsolutePath);

        /// <summary>
        /// The path of the given directory relative to this one.
        /// </summary>
        /// <param name="other">The directory to write the path of.</param>
        public string RelativePath(IDirectory other) => Relative(directory, other.AbsolutePath);
    }

    private static string Relative(IDirectory directory, string path) =>
        Path.GetRelativePath(directory.AbsolutePath, path).Replace(Path.DirectorySeparatorChar, '/');
}
