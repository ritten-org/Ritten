namespace Ritten.Bitwarden;

/// <summary>
/// What <c>bw status</c> prints: the server, who is logged in, and whether the vault is open.
/// </summary>
/// <param name="ServerUrl">The configured server; null for Bitwarden's cloud.</param>
/// <param name="LastSync">When the local copy last came from the server; null before it ever has.</param>
/// <param name="UserId">The logged-in user's id; null with nobody logged in.</param>
/// <param name="Status"><c>unauthenticated</c>, <c>locked</c> or <c>unlocked</c>.</param>
internal sealed record BitwardenStatus(string? ServerUrl, DateTimeOffset? LastSync, string? UserId, string Status)
{
    /// <summary>
    /// Nobody is logged in.
    /// </summary>
    public bool IsUnauthenticated => Status == "unauthenticated";

    /// <summary>
    /// The vault is open in the session the CLI was given.
    /// </summary>
    public bool IsUnlocked => Status == "unlocked";

    /// <summary>
    /// Whether the CLI talks to the given server, a trailing slash aside.
    /// </summary>
    public bool IsServer(string url) =>
        string.Equals(ServerUrl?.TrimEnd('/'), url.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
}
