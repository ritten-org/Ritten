namespace Ritten.OpenTofu;

/// <summary>
/// Exposes the OpenTofu root module the workflow is running against.
/// </summary>
public interface IOpenTofu
{
    /// <summary>
    /// Prepares the root module: downloads its providers and configures its backend.
    /// </summary>
    /// <param name="environment">The environment to prepare for, or null for the defaults.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task Init(TofuEnvironment? environment = null, CancellationToken ct = default);

    /// <summary>
    /// Works out what applying the configuration would change, without changing anything.
    /// </summary>
    /// <param name="environment">The environment to plan for, or null for the defaults.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task<TofuPlanResult> Plan(TofuEnvironment? environment = null, CancellationToken ct = default);

    /// <summary>
    /// Applies the configuration.
    /// </summary>
    /// <param name="environment">The environment to apply to, or null for the defaults.</param>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    Task Apply(TofuEnvironment? environment = null, CancellationToken ct = default);

    /// <summary>
    /// Checks that every file under the root module is formatted.
    /// </summary>
    /// <returns><c>null</c> when they all are, otherwise the files that are not.</returns>
    Task<IReadOnlyList<string>?> VerifyFormatting(CancellationToken ct = default);
}
