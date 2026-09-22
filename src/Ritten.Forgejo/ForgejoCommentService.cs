using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.Forgejo;

/// <summary>
/// The workflow's pull request comment, over Forgejo's REST API.
/// </summary>
internal sealed class ForgejoCommentService(
    IWorkflowLog log,
    IOptions<ForgejoActionsOptions> options,
    RunContext context,
    IHttpClientFactory clients
) : IForgejoCommentService
{
    /// <summary>
    /// The name of the configured client, which carries the instance's address and the run's token.
    /// </summary>
    internal const string HttpClientName = "forgejo";

    public async Task CreateOrUpdate(string body, CancellationToken cancellationToken = default)
    {
        if (options.Value.Repository is not { } repository || options.Value.ApiUrl is null)
        {
            log.Detail("The runner did not say which repository this is; skipping the pull request comment.");
            return;
        }

        // Forgejo hands the token to the workflow as a secret, and a secret is not an environment
        // variable until the workflow makes it one. Saying so is the whole value of this branch:
        // otherwise the only symptom is a comment that never appears.
        if (options.Value.Token is null)
        {
            log.Detail($"No API token in the environment; skipping the pull request comment. Pass one as {ForgejoEnvironment.Mirror(ForgejoEnvironment.Token)} to enable it.");
            return;
        }

        if (!options.Value.IsPullRequest)
        {
            throw new InvalidOperationException("Attempted to create or update a pull request comment outside of a pull request context.");
        }

        var number = options.Value.PullRequestNumber.Value;
        var fullBody = $"{Marker}\n{body}";
        var client = clients.CreateClient(HttpClientName);

        if (await FindExisting(client, repository, number, cancellationToken) is { } existing)
        {
            log.Detail($"Updating existing comment on PR #{number}.");
            var updated = await client.PatchAsJsonAsync($"repos/{repository}/issues/comments/{existing}", new CommentBody(fullBody), cancellationToken);
            updated.EnsureSuccessStatusCode();
            return;
        }

        log.Detail($"Creating comment on PR #{number}.");
        var created = await client.PostAsJsonAsync($"repos/{repository}/issues/{number}/comments", new CommentBody(fullBody), cancellationToken);
        created.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// The id of this workflow's comment, or <c>null</c> when it has not posted one yet.
    /// </summary>
    /// <remarks>
    /// Paged, because the API pages: a long-running pull request pushes the comment off the first
    /// page, and a search that stopped there would post a second one on every run from then on.
    /// </remarks>
    public async Task Delete(CancellationToken cancellationToken = default)
    {
        if (options.Value.Repository is not { } repository || options.Value.ApiUrl is null || options.Value.Token is null || !options.Value.IsPullRequest)
        {
            return;
        }

        var number = options.Value.PullRequestNumber.Value;
        var client = clients.CreateClient(HttpClientName);
        if (await FindExisting(client, repository, number, cancellationToken) is not { } existing)
        {
            return;
        }

        log.Detail($"Removing the comment on PR #{number}: nothing to say.");
        var deleted = await client.DeleteAsync($"repos/{repository}/issues/comments/{existing}", cancellationToken);
        deleted.EnsureSuccessStatusCode();
    }

    private async Task<long?> FindExisting(HttpClient client, string repository, int number, CancellationToken cancellationToken)
    {
        const int pageSize = 50;
        for (var page = 1; page <= MaxPages; page++)
        {
            var comments = await client.GetFromJsonAsync<Comment[]>(
                $"repos/{repository}/issues/{number}/comments?page={page}&limit={pageSize}", cancellationToken);
            if (comments is null or { Length: 0 })
            {
                return null;
            }

            if (Array.Find(comments, c => c.Body?.StartsWith(Marker, StringComparison.Ordinal) == true) is { } mine)
            {
                return mine.Id;
            }

            if (comments.Length < pageSize)
            {
                return null;
            }
        }

        // A thread this long is not worth another round trip; posting again is the safer end.
        log.Detail("Gave up looking for the existing comment after too many pages.");
        return null;
    }

    // One comment per workflow, found again on later runs by this invisible prefix.
    private string Marker => $"<!-- ritten:{Slug(context.Title)} -->";

    private const int MaxPages = 20;

    private static string Slug(string value) =>
        string.Concat(value.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-'));

    private sealed record Comment([property: JsonPropertyName("id")] long Id, [property: JsonPropertyName("body")] string? Body);

    private sealed record CommentBody([property: JsonPropertyName("body")] string Body);
}
