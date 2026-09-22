using Ritten.Contracts;

namespace Ritten.Engine.Runtimes;

/// <summary>
/// A no-op secret provider.
/// </summary>
internal sealed class DefaultSecretProviderProvider : ISecretProvider
{
    private static readonly string[] Schemes = ["op://", "bw://", "bws://", "vault://"];

    /// <inheritdoc />
    public Task<string> Resolve(string value, CancellationToken ct = default) =>
        Schemes.Any(scheme => value.StartsWith(scheme, StringComparison.Ordinal))
            ? throw new InvalidOperationException(
                $"'{value}' is a secret reference, and no secrets provider is registered to read it. " +
                "Register one on the application builder — Ritten.OnePassword's AddOnePassword(), for instance.")
            : Task.FromResult(value);
}
