namespace Ritten.OnePassword;

/// <summary>
/// How the 1Password CLI authenticates.
/// </summary>
public sealed class OnePasswordOptions
{
    /// <summary>
    /// A file holding a service-account token, read when <c>OP_SERVICE_ACCOUNT_TOKEN</c> is not
    /// in the environment; null to rely on the environment, or on a signed-in desktop app.
    /// </summary>
    public string? ServiceAccountTokenFile { get; set; }
}
