namespace Ritten.Contracts.FileSystem;

/// <summary>
/// Contains extension methods for <see cref="IFile"/>: reading and writing a file's whole text.
/// </summary>
public static class FileExtensions
{
    extension(IFile file)
    {
        /// <summary>
        /// Reads the file's whole text, or null when there is no file.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        public async Task<string?> ReadAllTextIfExists(CancellationToken cancellationToken = default)
        {
            if (!file.Exists)
            {
                return null;
            }

            using var reader = new StreamReader(file.OpenRead());
            return await reader.ReadToEndAsync(cancellationToken);
        }

        /// <summary>
        /// Replaces the file's text, creating the file and its directory when they don't exist.
        /// </summary>
        /// <param name="text">The file's whole new text.</param>
        /// <param name="atomic">Whether to write beside the file and rename over it.</param>
        /// <param name="mode">The Unix permissions the file ends with.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        public async Task WriteAllText(string text, bool atomic = true, UnixFileMode? mode = null, CancellationToken cancellationToken = default)
        {
            file.Directory.Create();
            var keep = mode ?? file.GetUnixFileMode();

            if (!atomic)
            {
                await WriteTo(file, text, keep, cancellationToken);
                return;
            }

            // Beside the file, so the rename stays on one volume and is atomic; hidden and unique,
            // so it is never mistaken for the file or collides with another writer's.
            var staged = file.Directory.GetFile($".{file.Name}.{Guid.NewGuid():N}.tmp");
            try
            {
                await WriteTo(staged, text, keep, cancellationToken);
                staged.MoveTo(file);
            }
            catch
            {
                staged.Delete();
                throw;
            }
        }
    }

    private static async Task WriteTo(IFile target, string text, UnixFileMode? mode, CancellationToken cancellationToken)
    {
        await using var stream = target.OpenWrite();
        // Before any text is written, so a secret is never readable under the wrong mode even
        // for the moment between the write and a chmod.
        if (mode is { } permissions)
        {
            target.SetUnixFileMode(permissions);
        }

        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(text.AsMemory(), cancellationToken);
    }
}
