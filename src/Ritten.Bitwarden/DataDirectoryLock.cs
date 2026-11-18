namespace Ritten.Bitwarden;

/// <summary>
/// One run at a time in the CLI's directory, across processes.
/// </summary>
/// <remarks>
/// Each <c>bw unlock</c> writes the vault's key into the directory, encrypted for the session it hands back, so a
/// second run unlocking in the same directory invalidates the first's session between its unlock and its list.
/// Runs on one machine — a runner taking several jobs — therefore take turns for the few seconds a read takes; the
/// lock is the file's, which .NET takes as an advisory <c>flock</c> on Unix, released with the process however it
/// ends.
/// </remarks>
internal sealed class DataDirectoryLock : IDisposable
{
    private const string FileName = "ritten.lock";
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(250);

    private readonly FileStream _file;

    private DataDirectoryLock(FileStream file) => _file = file;

    /// <summary>
    /// Waits for the directory, up to <paramref name="limit"/>.
    /// </summary>
    /// <exception cref="TimeoutException">Another run held it for longer.</exception>
    public static async Task<DataDirectoryLock> Acquire(string directory, TimeSpan limit, CancellationToken ct = default)
    {
        var path = Path.Combine(directory, FileName);
        var deadline = DateTimeOffset.UtcNow + limit;
        while (true)
        {
            try
            {
                return new DataDirectoryLock(new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
            }
            catch (IOException) when (DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(Interval, ct);
            }
            catch (IOException failure)
            {
                throw new TimeoutException($"Another run has held {path} for longer than {limit}.", failure);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose() => _file.Dispose();
}
