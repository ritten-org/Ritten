namespace Ritten.OpenTofu;

/// <summary>
/// Exposes the OpenTofu root module the workflow is running against.
/// </summary>
public interface IOpenTofu
{
    /// <summary>
    /// Prepares the root module: downloads its providers and configures its backend.
    /// </summary>
    Task Init(CancellationToken ct = default);

    /// <summary>
    /// Works out what applying the configuration would change, without changing anything.
    /// </summary>
    Task<TofuPlanResult> Plan(CancellationToken ct = default);

    /// <summary>
    /// Applies the configuration.
    /// </summary>
    Task Apply(CancellationToken ct = default);

    /// <summary>
    /// Checks that every file under the root module is formatted.
    /// </summary>
    /// <returns><c>null</c> when they all are, otherwise the files that are not.</returns>
    Task<IReadOnlyList<string>?> VerifyFormatting(CancellationToken ct = default);
}
