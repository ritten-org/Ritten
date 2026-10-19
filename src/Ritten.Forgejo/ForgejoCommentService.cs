using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Ritten.Reporting;

namespace Ritten.Forgejo;

/// <summary>
/// The workflow's pull request comments, over Forgejo's REST API.
/// </summary>
internal sealed class ForgejoCommentService(IOptions<ForgejoActionsOptions> options, IHttpClientFactory clients) : IForgejoCommentService
{
    /// <summary>
    /// The name of the configured client, which carries the instance's address and the run's token.
    /// </summary>
    internal const string HttpClientName = "forgejo";

    private const string MarkerPrefix = "<!-- ritten:";
    private const string MarkerSuffix = " -->";
    private const int PageSize = 50;
    private const int MaxPages = 20;

    /// <inheritdoc />
    public string? Unavailable => options.Value switch
    {
        { Repository: null } or { ApiUrl: null } => "The runner did not say which repository this is; skipping the pull request comment.",

        // Forgejo hands the token to the workflow as a secret, and a secret is not an environment
        // variable until the workflow makes it one. Saying so is the whole value of this branch:
        // otherwise the only symptom is a comment that never appears.
        { Token: null } => $"No API token in the environment; skipping the pull request comment. Set {ForgejoEnvironment.Token} to enable it.",
        _ => null
    };

    /// <inheritdoc />
    public async Task CreateOrUpdate(ReportKey key, string body, CancellationToken cancellationToken = default)
    {
        if (Unavailable is not null)
        {
            return;
        }

        if (!options.Value.IsPullRequest)
        {
            throw new InvalidOperationException("Attempted to create or update a pull request comment outside of a pull request context.");
        }

        var (repository, number) = (options.Value.Repository!, options.Value.PullRequestNumber.Value);
        var fullBody = $"{MarkerOf(key)}\n{body}";
        var client = clients.CreateClient(HttpClientName);

        if (await Find(client, key, cancellationToken) is { } existing)
        {
            var updated = await client.PatchAsJsonAsync($"repos/{repository}/issues/comments/{existing.Id}", new CommentBody(fullBody), cancellationToken);
            updated.EnsureSuccessStatusCode();
            return;
        }

        var created = await client.PostAsJsonAsync($"repos/{repository}/issues/{number}/comments", new CommentBody(fullBody), cancellationToken);
        created.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task Delete(ReportKey key, CancellationToken cancellationToken = default)
    {
        if (Unavailable is not null || !options.Value.IsPullRequest)
        {
            return;
        }

        var client = clients.CreateClient(HttpClientName);
        if (await Find(client, key, cancellationToken) is not { } existing)
        {
            return;
        }

        var deleted = await client.DeleteAsync($"repos/{options.Value.Repository}/issues/comments/{existing.Id}", cancellationToken);
        deleted.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReportKey>> List(CancellationToken cancellationToken = default)
    {
        if (Unavailable is not null || !options.Value.IsPullRequest)
        {
            return [];
        }

        var keys = new List<ReportKey>();
        await foreach (var comment in Comments(clients.CreateClient(HttpClientName), cancellationToken))
        {
            if (KeyOf(comment) is { } key)
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    /// <summary>
    /// The invisible prefix a report's comment is found again by on later runs.
    /// </summary>
    internal static string MarkerOf(ReportKey key) => $"{MarkerPrefix}{key}{MarkerSuffix}";

    private static ReportKey? KeyOf(Comment comment)
    {
        if (comment.Body is not { } body || !body.StartsWith(MarkerPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var end = body.IndexOf(MarkerSuffix, MarkerPrefix.Length, StringComparison.Ordinal);
        return end < 0 ? null : ReportKey.Parse(body[MarkerPrefix.Length..end]);
    }

    private async Task<Comment?> Find(HttpClient client, ReportKey key, CancellationToken cancellationToken)
    {
        await foreach (var comment in Comments(client, cancellationToken))
        {
            if (KeyOf(comment) == key)
            {
                return comment;
            }
        }

        return null;
    }

    /// <summary>
    /// The pull request's comments, paged, because the API pages: a long-running pull request pushes a comment off
    /// the first page. A thread longer than <see cref="MaxPages"/> is not worth another round trip.
    /// </summary>
    private async IAsyncEnumerable<Comment> Comments(HttpClient client, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var (repository, number) = (options.Value.Repository, options.Value.PullRequestNumber);
        for (var page = 1; page <= MaxPages; page++)
        {
            var comments = await client.GetFromJsonAsync<Comment[]>(
                $"repos/{repository}/issues/{number}/comments?page={page}&limit={PageSize}", cancellationToken);
            if (comments is null or { Length: 0 })
            {
                yield break;
            }

            foreach (var comment in comments)
            {
                yield return comment;
            }

            if (comments.Length < PageSize)
            {
                yield break;
            }
        }
    }

    private sealed record Comment([property: JsonPropertyName("id")] long Id, [property: JsonPropertyName("body")] string? Body);

    private sealed record CommentBody([property: JsonPropertyName("body")] string Body);
}
