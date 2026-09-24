namespace Ritten.Contracts.FileSystem;

/// <summary>
/// Represents a file in the file system.
/// </summary>
public interface IFile
{
    /// <summary>
    /// Gets the name of the file, including the extension.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the name of the file, excluding the extension.
    /// </summary>
    string NameWithoutExtension { get; }

    /// <summary>
    /// Gets extension part of the file's name, including the separator character (usually a period).
    /// </summary>
    string Extension { get; }

    /// <summary>
    /// Gets the full path to the file.
    /// </summary>
    string AbsolutePath { get; }

    /// <summary>
    /// Gets a value indicating whether this file exists in the file system.
    /// </summary>
    bool Exists { get; }

    /// <summary>
    /// Gets the directory the file is in, whether or not either exists yet.
    /// </summary>
    IDirectory Directory { get; }

    /// <summary>
    /// Deletes the file from the file system if it exists.
    /// </summary>
    void Delete();

    /// <summary>
    /// Opens the file for reading.
    /// </summary>
    /// <returns>A stream that can be read from.</returns>
    Stream OpenRead();

    /// <summary>
    /// Opens the file for writing, creating or overwriting it as necessary.
    /// </summary>
    /// <returns>A stream that can be written to.</returns>
    Stream OpenWrite();

    /// <summary>
    /// Moves this file to <paramref name="destination"/>, replacing it if it exists.
    /// </summary>
    /// <param name="destination">Where the file goes.</param>
    void MoveTo(IFile destination);

    /// <summary>
    /// Gets the file's Unix permissions, or null when it does not exist or the platform has none.
    /// </summary>
    UnixFileMode? GetUnixFileMode();

    /// <summary>
    /// Sets the file's Unix permissions. Does nothing on a platform without them.
    /// </summary>
    /// <param name="mode">The permissions.</param>
    void SetUnixFileMode(UnixFileMode mode);
}
