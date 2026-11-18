namespace Ritten.Bitwarden;

/// <summary>
/// A login item's own username and password.
/// </summary>
/// <param name="Username">The username.</param>
/// <param name="Password">The password.</param>
internal sealed record BitwardenLogin(string? Username, string? Password);
