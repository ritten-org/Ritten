using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Engine.Runtimes;
using Ritten.Reporting.Sinks;

namespace Ritten.GitHub;

/// <summary>
/// The GitHub Actions runtime.
/// </summary>
public sealed class GitHubActionsRuntime : Runtime
{
    /// <inheritdoc />
    public override string Name => "github-actions";

    /// <inheritdoc />
    public override IReadOnlyCollection<string> Markers { get; } = [GitHubEnvironment.Actions];

    // GH_TOKEN is deliberately absent: it's a user's explicit instruction to the GitHub client,
    // not something the runner provides, so it stays visible to the destination configuration.
    /// <inheritdoc />
    public override IReadOnlyCollection<string> Claims { get; } =
    [
        GitHubEnvironment.Actions,
        GitHubEnvironment.DefaultToken,
        GitHubEnvironment.RepositoryId,
        GitHubEnvironment.Ref,
        GitHubEnvironment.BaseRef,
        GitHubEnvironment.StepSummary,
        GitHubEnvironment.Workflow,
        GitHubEnvironment.ServerUrl,
        GitHubEnvironment.Repository,
        GitHubEnvironment.RunId,
        GitHubEnvironment.RunnerDebug
    ];

    /// <inheritdoc />
    public override bool IsDebug(Func<string, string?> environment) => GitHubEnvironment.IsDebug(environment);

    /// <inheritdoc />
    public override void Configure(IWorkflowBuilder builder, Func<string, string?> environment)
    {
        // The runtime's comment and release plumbing talk to the GitHub API, so it brings the
        // client with it rather than requiring every job on this runtime to know that.
        builder.AddGitHubClient();

        builder.Services.AddOptions<GitHubActionsOptions>()
            .Configure(options => GitHubActionsOptions.ConfigureFromEnvironment(options, environment));

        if (environment(GitHubEnvironment.Workflow) is { } workflow)
        {
            builder.Services.TryAddSingleton(new RunContext { Title = workflow });
        }

        var actions = new GitHubActionsOptions();
        GitHubActionsOptions.ConfigureFromEnvironment(actions, environment);
        builder.Services.TryAddSingleton(new PullRequest { Number = actions.PullRequestNumber, BaseRef = actions.BaseRef });

        // The workflow's own token backs the GitHub client when no explicit GH_TOKEN is given.
        // Other runtimes define GITHUB_TOKEN for workflow compatibility with a token for *their*
        // forge, which is exactly why the variable is claimed: only the runtime it belongs to may
        // offer it, and only this runtime's belongs to GitHub.
        if (environment(GitHubEnvironment.DefaultToken) is { } token)
        {
            builder.Services.PostConfigure<GitHubClientOptions>(options => options.Token ??= token);
        }

        builder.Services.TryAddSingleton<IPullRequestLabels, GitHubPullRequestLabels>();

        builder.Services.TryAddSingleton<IGitHubCommentService, GitHubGitHubCommentService>();
        builder.Decorators.Replace<IGitHubCommentService, GitHubDryRunCommentService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowResultSink, GitHubJobSummaryResultSink>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowResultSink, GitHubCommentResultSink>());
    }
}
