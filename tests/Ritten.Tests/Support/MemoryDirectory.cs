using Ritten.Contracts.FileSystem;

namespace Ritten.Tests.Support;

/// <summary>
/// A directory held in memory: the files it hands out stay the same objects, so a staged sibling
/// and the file it replaces can see each other.
/// </summary>
public sealed class MemoryDirectory(string path) : IDirectory
{
    private readonly Dictionary<string, MemoryFile> _files = new(StringComparer.Ordinal);

    /// <summary>Whether <see cref="Create"/> was called, or a file was written here.</summary>
    public bool Created { get; private set; }

    /// <summary>The file named <paramref name="name"/>, as the concrete type a test inspects.</summary>
    public MemoryFile File(string name)
    {
        if (!_files.TryGetValue(name, out var file))
        {
            file = new MemoryFile(this, name);
            _files[name] = file;
        }

        return file;
    }

    /// <inheritdoc />
    public string Name => Path.GetFileName(path);

    /// <inheritdoc />
    public string AbsolutePath => path;

    /// <inheritdoc />
    public bool Exists => Created || _files.Values.Any(f => f.Exists);

    /// <inheritdoc />
    public DateTimeOffset LastWriteTime => _files.Values.Where(f => f.Exists).Select(f => f.LastWriteTime).DefaultIfEmpty().Max();

    /// <inheritdoc />
    public void Create() => Created = true;

    /// <inheritdoc />
    public void Delete()
    {
        foreach (var file in _files.Values)
        {
            file.Delete();
        }

        Created = false;
    }

    /// <inheritdoc />
    public IFile GetFile(string name) => File(name);

    /// <inheritdoc />
    public IDirectory GetDirectory(string name) => new MemoryDirectory($"{path}/{name}");

    /// <inheritdoc />
    public IEnumerable<IFile> GetFiles(string searchPattern = "*") => _files.Values.Where(f => f.Exists);

    /// <inheritdoc />
    public IEnumerable<IDirectory> GetDirectories(bool recursive = false) => [];

    /// <inheritdoc />
    public void MoveTo(IDirectory destination) => throw new NotSupportedException("A memory directory stays where it is.");

    /// <summary>The files that exist here, by name — what a test checks nothing was left behind in.</summary>
    public IEnumerable<string> FileNames => _files.Values.Where(f => f.Exists).Select(f => f.Name);
}
