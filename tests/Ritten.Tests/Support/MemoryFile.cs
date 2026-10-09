using System.Text;
using Ritten.Contracts.FileSystem;

namespace Ritten.Tests.Support;

/// <summary>
/// A file held in memory, for steps that read and write through <see cref="IFile"/>. It behaves
/// like the real thing where a write's shape matters — a staged sibling, a rename over the file —
/// so a test asserts on what the file ends up holding rather than on how it was written.
/// </summary>
public sealed class MemoryFile : IFile
{
    private readonly MemoryDirectory _directory;

    internal MemoryFile(MemoryDirectory directory, string name)
    {
        _directory = directory;
        Name = name;
    }

    /// <summary>A file named <paramref name="name"/> that holds <paramref name="text"/>.</summary>
    public static MemoryFile Existing(string name, string text)
    {
        var file = new MemoryDirectory("/memory").File(name);
        file.Text = text;
        return file;
    }

    /// <summary>A file named <paramref name="name"/> that does not exist yet.</summary>
    public static MemoryFile Missing(string name) => new MemoryDirectory("/memory").File(name);

    /// <summary>The file's text, or null when it does not exist.</summary>
    public string? Text { get; private set; }

    /// <summary>How many times the file's contents were replaced — written in place, or moved onto.</summary>
    public int Writes { get; private set; }

    /// <summary>The file's Unix permissions, when it has been given any.</summary>
    public UnixFileMode? Mode { get; private set; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string NameWithoutExtension => Path.GetFileNameWithoutExtension(Name);

    /// <inheritdoc />
    public string Extension => Path.GetExtension(Name);

    /// <inheritdoc />
    public string AbsolutePath => $"{_directory.AbsolutePath}/{Name}";

    /// <inheritdoc />
    public bool Exists => Text is not null;

    /// <inheritdoc />
    public DateTimeOffset LastWriteTime { get; private set; }

    /// <inheritdoc />
    public IDirectory Directory => _directory;

    /// <inheritdoc />
    public void Delete()
    {
        Text = null;
        Mode = null;
    }

    /// <inheritdoc />
    public Stream OpenRead() =>
        Text is { } text ? new MemoryStream(Encoding.UTF8.GetBytes(text)) : throw new FileNotFoundException(AbsolutePath);

    /// <inheritdoc />
    public Stream OpenWrite()
    {
        Text = "";
        return new LandingStream(bytes =>
        {
            Text = Encoding.UTF8.GetString(bytes);
            LastWriteTime = DateTimeOffset.UtcNow;
            Writes++;
        });
    }

    /// <inheritdoc />
    public void MoveTo(IFile destination)
    {
        var target = (MemoryFile)destination;
        target.Text = Text;
        target.Mode = Mode;
        target.LastWriteTime = LastWriteTime;
        target.Writes++;
        Delete();
    }

    /// <inheritdoc />
    public UnixFileMode? GetUnixFileMode() => Exists ? Mode : null;

    /// <inheritdoc />
    public void SetUnixFileMode(UnixFileMode mode) => Mode = mode;

    /// <summary>Lands what was written when the writer lets go of it.</summary>
    private sealed class LandingStream(Action<byte[]> land) : MemoryStream
    {
        private bool _landed;

        protected override void Dispose(bool disposing)
        {
            if (!_landed)
            {
                _landed = true;
                land(ToArray());
            }

            base.Dispose(disposing);
        }
    }
}