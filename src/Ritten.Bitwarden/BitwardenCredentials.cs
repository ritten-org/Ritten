using Ritten.Commands;

namespace Ritten.Bitwarden;

/// <summary>
/// A user the CLI logs in as with an API key and unlocks with its master password: a machine's identity, since
/// Bitwarden has no other kind outside its Secrets Manager.
/// </summary>
/// <param name="ClientId">The API key's <c>client_id</c>, <c>user.</c> and the user's id.</param>
/// <param name="ClientSecret">The API key's <c>client_secret</c>.</param>
/// <param name="Password">The user's master password.</param>
internal sealed record BitwardenCredentials(string ClientId, string ClientSecret, string Password)
{
    internal const string ClientIdVariable = "BW_CLIENTID";
    internal const string ClientSecretVariable = "BW_CLIENTSECRET";
    internal const string PasswordVariable = "BW_PASSWORD";

    private static readonly string[] Variables = [ClientIdVariable, ClientSecretVariable, PasswordVariable];

    /// <summary>
    /// The user's id, which <c>bw status</c> reports for whoever is logged in.
    /// </summary>
    public string UserId => ClientId.StartsWith("user.", StringComparison.Ordinal) ? ClientId["user.".Length..] : ClientId;

    /// <summary>
    /// The credentials from the environment when it holds all three, else from the file; null when neither has
    /// any, which leaves a session someone unlocked.
    /// </summary>
    /// <exception cref="InvalidOperationException">The file is there but incomplete or malformed.</exception>
    public static BitwardenCredentials? Find(Func<string, string?> environment, string? file)
    {
        if (From(environment) is { } fromEnvironment)
        {
            return fromEnvironment;
        }

        if (file is null || ExpandHome(file) is not { } path || !File.Exists(path))
        {
            return null;
        }

        // Read literally: a master password with a $ or a backslash in it is changed by a shell that sources
        // the file, so the file is parsed as an env file, never run.
        var parsed = EnvironmentFile.Parse(File.ReadAllText(path), path);
        if (parsed.IsError)
        {
            throw new InvalidOperationException(string.Join(' ', parsed.Errors.Select(error => error.Message)));
        }

        var missing = Variables.Where(name => parsed.Value.GetValueOrDefault(name) is not { Length: > 0 }).ToList();
        return missing.Count == 0
            ? From(name => parsed.Value.GetValueOrDefault(name))
            : throw new InvalidOperationException($"{path} has no {string.Join(", ", missing)}.");
    }

    /// <summary>
    /// A path with a leading <c>~/</c> made the user's home.
    /// </summary>
    internal static string ExpandHome(string path) =>
        path.StartsWith("~/", StringComparison.Ordinal)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[2..])
            : path;

    private static BitwardenCredentials? From(Func<string, string?> read) =>
        read(ClientIdVariable) is { Length: > 0 } id
        && read(ClientSecretVariable) is { Length: > 0 } secret
        && read(PasswordVariable) is { Length: > 0 } password
            ? new BitwardenCredentials(id, secret, password)
            : null;
}
