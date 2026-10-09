namespace Ritten.OpenTofu;

/// <summary>
/// Runs OpenTofu against a root module.
/// </summary>
public interface IOpenTofu
{
    /// <summary>
    /// Prepares the root module: downloads its providers and configures its backend.
    /// </summary>
    /// <param name="module">The root module.</param>
    /// <param name="environment">The environment to prepare for, or null for the defaults.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task Init(TofuModule module, TofuEnvironment? environment = null, CancellationToken ct = default);

    /// <summary>
    /// Works out what applying the configuration would change, without changing anything.
    /// </summary>
    /// <param name="module">The root module.</param>
    /// <param name="environment">The environment to plan for, or null for the defaults.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task<TofuPlanResult> Plan(TofuModule module, TofuEnvironment? environment = null, CancellationToken ct = default);

    /// <summary>
    /// Applies the configuration.
    /// </summary>
    /// <param name="module">The root module.</param>
    /// <param name="environment">The environment to apply to, or null for the defaults.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task Apply(TofuModule module, TofuEnvironment? environment = null, CancellationToken ct = default);

    /// <summary>
    /// Checks that every file under the root module is formatted.
    /// </summary>
    /// <param name="module">The root module.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns><c>null</c> when they all are, otherwise the files that are not.</returns>
    Task<IReadOnlyList<string>?> VerifyFormatting(TofuModule module, CancellationToken ct = default);
}
