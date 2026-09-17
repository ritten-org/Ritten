namespace Ritten.Git;

/// <summary>
/// What a push over HTTP authenticates with. A token goes in <see cref="Password"/>; the
/// username is whatever the host expects beside it, which for a personal access token is
/// usually the account it was minted for.
/// </summary>
/// <param name="Username">The username to present.</param>
/// <param name="Password">The password or token to present.</param>
public sealed record GitCredential(string Username, string Password);
