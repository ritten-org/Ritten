namespace Ritten.Bitwarden;

/// <summary>
/// Which server the CLI talks to, and as whom.
/// </summary>
public sealed class BitwardenOptions
{
    /// <summary>
    /// The server, such as a Vaultwarden's <c>https://vault.example.com</c>; null for whatever the CLI is already
    /// configured with, which is Bitwarden's own cloud unless someone said otherwise.
    /// </summary>
    public string? ServerUrl { get; set; }

    /// <summary>
    /// An env file holding <c>BW_CLIENTID</c>, <c>BW_CLIENTSECRET</c> and <c>BW_PASSWORD</c>, read when the
    /// environment does not hold all three; null to rely on the environment, or on a session someone unlocked.
    /// </summary>
    public string? CredentialsFile { get; set; }

    /// <summary>
    /// The CLI's own directory (<c>BITWARDENCLI_APPDATA_DIR</c>): its login and its encrypted copy of the vault.
    /// Null for the CLI's default, which a person's own <c>bw</c> uses too.
    /// </summary>
    /// <remarks>
    /// A machine logging in with credentials should have one of its own, kept between runs: it is what is read
    /// when the server cannot be reached, and runs sharing it take turns.
    /// </remarks>
    public string? DataDirectory { get; set; }
}
