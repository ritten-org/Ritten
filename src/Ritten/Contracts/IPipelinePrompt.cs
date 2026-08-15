namespace Ritten.Contracts;

/// <summary>
/// Asks whoever is running the pipeline to approve something before it happens.
/// </summary>
public interface IPipelinePrompt
{
    /// <summary>
    /// Whether there's anyone there to ask. False on a build agent, where stdin isn't a terminal.
    /// </summary>
    bool IsInteractive { get; }

    /// <summary>
    /// Asks for approval, describing what is about to happen.
    /// Only <c>yes</c> approves; anything else, including an empty line, declines.
    /// </summary>
    /// <param name="consequence">What is about to happen, phrased so that declining is informed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<bool> Confirm(string consequence, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks for a secret value, read without echoing it to the terminal.
    /// Returns null when nothing is entered.
    /// </summary>
    /// <param name="what">What is being asked for, shown as the prompt.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task<string?> Secret(string what, CancellationToken cancellationToken = default);
}
