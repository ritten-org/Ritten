using System.Text.Json;
using Microsoft.Extensions.Options;
using Ritten.Commands;
using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.Bitwarden;

/// <summary>
/// Reads secrets through the Bitwarden CLI by <c>bw://collection/item/field</c> reference, from a Bitwarden or
/// Vaultwarden server.
/// </summary>
/// <remarks>
/// <para>
/// Every <c>bw</c> call costs seconds, and a lookup by name is a search, so the vault is read whole the first
/// time a run needs a secret — logged in, unlocked, synced, listed — and every reference is answered from that,
/// by exact name. The run holds the decrypted vault in memory and nothing of it on disk.
/// </para>
/// <para>
/// A machine logs in as its own user, with an API key and the user's master password (<see cref="BitwardenCredentials"/>).
/// With no credentials, a session someone unlocked (<c>BW_SESSION</c>) is used as it is: a person at a desk reads as
/// themselves.
/// </para>
/// </remarks>
/// <param name="commands">The command runner.</param>
/// <param name="options">The server, the credentials and the CLI's directory.</param>
/// <param name="log">The run's narrative, for a sync that failed.</param>
/// <param name="environment">The process environment; tests give their own.</param>
internal sealed class BitwardenSecretProvider(
    ICommandRunner commands,
    IOptions<BitwardenOptions> options,
    IWorkflowLog log,
    Func<string, string?>? environment = null) : ISecretProvider, IDisposable
{
    private const string Executable = "bw";
    private const string DataDirectoryVariable = "BITWARDENCLI_APPDATA_DIR";
    private const string SessionVariable = "BW_SESSION";
    private static readonly TimeSpan LockLimit = TimeSpan.FromMinutes(2);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly Func<string, string?> _environment = environment ?? Environment.GetEnvironmentVariable;
    private readonly SemaphoreSlim _loading = new(1, 1);
    private BitwardenVault? _vault;

    /// <inheritdoc />
    public async Task<string> Resolve(string value, CancellationToken ct = default)
    {
        if (BitwardenReference.Parse(value) is not { } reference)
        {
            return value;
        }

        return (await Vault(ct)).Read(reference);
    }

    /// <inheritdoc />
    public void Dispose() => _loading.Dispose();

    private async Task<BitwardenVault> Vault(CancellationToken ct)
    {
        await _loading.WaitAsync(ct);
        try
        {
            return _vault ??= await Load(ct);
        }
        finally
        {
            _loading.Release();
        }
    }

    private async Task<BitwardenVault> Load(CancellationToken ct)
    {
        var settings = options.Value;
        var credentials = BitwardenCredentials.Find(_environment, settings.CredentialsFile);
        var directory = settings.DataDirectory is { Length: > 0 } configured ? BitwardenCredentials.ExpandHome(configured) : null;
        var environment = new Dictionary<string, string>();
        if (directory is not null)
        {
            Create(directory);
            environment[DataDirectoryVariable] = directory;
        }

        using var _ = directory is null ? null : await DataDirectoryLock.Acquire(directory, LockLimit, ct);
        return credentials is null
            ? await FromSession(settings, environment, ct)
            : await AsMachine(credentials, settings, environment, ct);
    }

    // The directory holds the login and the encrypted copy: its owner's alone.
    private static void Create(string directory)
    {
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(directory);
        }
        else
        {
            Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private async Task<BitwardenVault> AsMachine(
        BitwardenCredentials credentials,
        BitwardenOptions settings,
        Dictionary<string, string> environment,
        CancellationToken ct)
    {
        // A session the process inherited is someone else's: the machine opens its own.
        environment[SessionVariable] = string.Empty;
        var status = await Status(environment, ct);

        // The server can only be changed logged out, and a login is per user: whichever is wrong, start again.
        var server = settings.ServerUrl is { Length: > 0 } url && !status.IsServer(url) ? url : null;
        if (!status.IsUnauthenticated && (server is not null || status.UserId != credentials.UserId))
        {
            await Bw(environment, ct, "logout");
            status = status with { Status = "unauthenticated", UserId = null };
        }

        if (server is not null)
        {
            await Bw(environment, ct, "config", "server", server);
        }

        if (status.IsUnauthenticated)
        {
            await Bw(
                new Dictionary<string, string>(environment)
                {
                    [BitwardenCredentials.ClientIdVariable] = credentials.ClientId,
                    [BitwardenCredentials.ClientSecretVariable] = credentials.ClientSecret,
                },
                ct,
                "login",
                "--apikey");
        }

        var unlocked = await Bw(
            new Dictionary<string, string>(environment) { [BitwardenCredentials.PasswordVariable] = credentials.Password },
            ct,
            "unlock",
            "--passwordenv",
            BitwardenCredentials.PasswordVariable,
            "--raw");
        environment[SessionVariable] = unlocked.Trim();

        return await Read(environment, status, ct);
    }

    private async Task<BitwardenVault> FromSession(BitwardenOptions settings, Dictionary<string, string> environment, CancellationToken ct)
    {
        var status = await Status(environment, ct);
        if (!status.IsUnlocked)
        {
            throw new InvalidOperationException(
                $"There are no Bitwarden credentials in the environment{(settings.CredentialsFile is { } file ? $" or {file}" : "")}, " +
                "and bw has no unlocked session: run `export BW_SESSION=\"$(bw unlock --raw)\"` first.");
        }

        // Someone's own CLI is theirs to point somewhere else; it is never reconfigured under them.
        if (settings.ServerUrl is { Length: > 0 } url && !status.IsServer(url))
        {
            throw new InvalidOperationException($"bw is configured for {status.ServerUrl ?? "Bitwarden's cloud"}, not {url}.");
        }

        return await Read(environment, status, ct);
    }

    private async Task<BitwardenVault> Read(Dictionary<string, string> environment, BitwardenStatus status, CancellationToken ct)
    {
        // A server that cannot be reached leaves the copy from the last sync, which is what a machine reads its
        // secrets from through an outage of the server or anything in front of it. A secret changed since is read
        // as it was, so the run is told.
        var synced = await commands.Run(BwCommand(environment, "sync"), ct);
        if (synced.IsError)
        {
            if (status.LastSync is not { } last)
            {
                throw new CommandFailedException(
                    $"bw could not sync, and has no copy of the vault to read: {string.Join(' ', synced.ErrorTail(3))}",
                    synced);
            }

            log.Warning($"bw could not sync ({string.Join(' ', synced.ErrorTail(1))}): reading the copy from {last:yyyy-MM-dd HH:mm} UTC.");
        }

        var collections = JsonSerializer.Deserialize<IReadOnlyList<BitwardenCollection>>(
            await Bw(environment, ct, "list", "collections"), Json) ?? [];
        var items = JsonSerializer.Deserialize<IReadOnlyList<BitwardenItem>>(
            await Bw(environment, ct, "list", "items"), Json) ?? [];
        log.Verbose($"Read {items.Count} item{(items.Count == 1 ? "" : "s")} in {collections.Count} collection{(collections.Count == 1 ? "" : "s")} from bw.");
        return new BitwardenVault(collections, items);
    }

    private async Task<BitwardenStatus> Status(Dictionary<string, string> environment, CancellationToken ct) =>
        JsonSerializer.Deserialize<BitwardenStatus>(await Bw(environment, ct, "status"), Json)
        ?? throw new InvalidOperationException("bw status printed nothing.");

    private async Task<string> Bw(IReadOnlyDictionary<string, string> environment, CancellationToken ct, params string[] arguments)
    {
        var result = await commands.Run(BwCommand(environment, arguments), ct);
        if (result.IsError)
        {
            // Redaction keeps the runner's own failure text to the exit code. What bw writes to stderr names the
            // item, the server or the reason, never a value, and that is the message worth having.
            var why = result.ErrorTail(3);
            throw new CommandFailedException(
                $"bw {arguments[0]} failed: {(why.Count == 0 ? $"exit code {result.ExitCode}" : string.Join(' ', why))}",
                result);
        }

        return result.StandardOutput;
    }

    // Every call's output is redacted: a list holds every secret, an unlock the session that opens them. And
    // none asks anything: a run has nobody to answer.
    private static Command BwCommand(IReadOnlyDictionary<string, string> environment, params string[] arguments) =>
        Command.Create(Executable)
            .WithArguments([.. arguments, "--nointeraction"])
            .WithEnvironmentVariables(environment)
            .RedactOutput()
            .QuietOutput();
}
