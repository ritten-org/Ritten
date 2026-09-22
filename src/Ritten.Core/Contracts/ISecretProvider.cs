namespace Ritten.Contracts;

/// <summary>
/// Provides an abstraction for resolving secrets.
/// </summary>
public interface ISecretProvider
{
    /// <summary>
    /// Resolves a value: a reference to the secret it names, a literal to itself.
    /// </summary>
    /// <param name="value">The value, as an env file or a setting spells it.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task<string> Resolve(string value, CancellationToken ct = default);
}
