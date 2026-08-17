using Microsoft.Extensions.Options;
using Octokit;
using Ritten.Commands;
using Ritten.Contracts;

namespace Ritten.GitHub;

/// <summary>
/// Resolves GitHub API credentials from wherever the run keeps them: the configured token first
/// (an explicit <c>GH_TOKEN</c>, or the ambient token the active runtime offered), then the gh
/// CLI's stored login, so running locally works without any setup beyond being logged in to gh.
/// </summary>
internal class AmbientCredentialStore(IPipelineLog log, IOptions<GitHubClientOptions> options, ICommandRunner commands) : ICredentialStore
{
    private Task<Credentials>? _credentials;

    /// <inheritdoc />
    public Task<Credentials> GetCredentials() => _credentials ??= Resolve();

    private async Task<Credentials> Resolve()
    {
        if (options.Value.Token is { } token)
        {
            return new Credentials(token);
        }

        try
        {
            var result = await commands.Run(Command.Create("gh").WithArguments("auth", "token").RedactOutput());
            if (result.IsSuccess && result.StandardOutput.Trim() is { Length: > 0 } ambient)
            {
                log.Verbose("Using the gh CLI's stored login for the GitHub API.");
                return new Credentials(ambient);
            }
        }
        catch (Exception)
        {
            // The gh CLI not being installed is fine; the API is still reachable anonymously.
        }

        log.Verbose("No GitHub credentials found; using the API anonymously.");
        return Credentials.Anonymous;
    }
}
