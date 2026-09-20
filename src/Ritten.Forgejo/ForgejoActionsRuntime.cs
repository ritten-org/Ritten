using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Engine.Runtimes;
using Ritten.Reporting;
using Ritten.Reporting.Sinks;

namespace Ritten.Forgejo;

/// <summary>
/// A job on a Forgejo runner.
/// </summary>
/// <remarks>
/// The runner mirrors every variable under GitHub's names for
/// workflows written against GitHub, so this runtime claims those too: when a host registers
/// both runtimes, the one that owns the native names wins the mirrored ones as well.
/// Not sealed, so a host can add what its runs need on top — a runtime is where a
/// run-dependent service belongs.
/// </remarks>
public class ForgejoActionsRuntime : Runtime
{
    /// <inheritdoc />
    public override string Name => "forgejo-actions";

    /// <inheritdoc />
    public override IReadOnlyCollection<string> Markers { get; } = [ForgejoEnvironment.Actions];

    /// <inheritdoc />
    public override IReadOnlyCollection<string> Claims { get; } =
    [
        ForgejoEnvironment.Actions,
        ForgejoEnvironment.GitHubActions,
        ForgejoEnvironment.ServerUrl,
        ForgejoEnvironment.Mirror(ForgejoEnvironment.ServerUrl),
        ForgejoEnvironment.Repository,
        ForgejoEnvironment.Mirror(ForgejoEnvironment.Repository),
        ForgejoEnvironment.RunNumber,
        ForgejoEnvironment.Mirror(ForgejoEnvironment.RunNumber),
        ForgejoEnvironment.RunId,
        ForgejoEnvironment.Mirror(ForgejoEnvironment.RunId),
        ForgejoEnvironment.Workflow,
        ForgejoEnvironment.Mirror(ForgejoEnvironment.Workflow),
        ForgejoEnvironment.Ref,
        ForgejoEnvironment.Mirror(ForgejoEnvironment.Ref),
        ForgejoEnvironment.BaseRef,
        ForgejoEnvironment.Mirror(ForgejoEnvironment.BaseRef),
        ForgejoEnvironment.Token,
        ForgejoEnvironment.Mirror(ForgejoEnvironment.Token),
        ForgejoEnvironment.StepSummary,
        ForgejoEnvironment.RunnerDebug
    ];

    /// <inheritdoc />
    public override bool IsDebug(Func<string, string?> environment) => ForgejoEnvironment.IsDebug(environment);

    /// <inheritdoc />
    public override void Configure(IWorkflowBuilder builder, Func<string, string?> environment)
    {
        // Sinks hear about a run through the report publisher, which build reporting registers.
        builder.AddBuildReporting();

        builder.Services.AddOptions<ForgejoActionsOptions>()
            .Configure(options => ForgejoActionsOptions.ConfigureFromEnvironment(options, environment));

        if (ForgejoEnvironment.Read(environment, ForgejoEnvironment.Workflow) is { } workflow)
        {
            builder.Services.TryAddSingleton(new RunContext { Title = workflow });
        }

        // Read once here as well as through options: what the runtime publishes about the pull
        // request is a fact of the run, so it is available to a step that never looks at Forgejo.
        var actions = new ForgejoActionsOptions();
        ForgejoActionsOptions.ConfigureFromEnvironment(actions, environment);
        builder.Services.TryAddSingleton(new PullRequest { Number = actions.PullRequestNumber, BaseRef = actions.BaseRef });

        builder.Services.AddHttpClient(ForgejoCommentService.HttpClientName, (provider, client) =>
        {
            var forgejo = provider.GetRequiredService<IOptions<ForgejoActionsOptions>>().Value;
            if (forgejo.ApiUrl is { } apiUrl)
            {
                client.BaseAddress = new Uri(apiUrl);
            }

            if (forgejo.Token is { } token)
            {
                // Forgejo's own scheme, not Bearer: a personal or workflow token, presented as-is.
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", token);
            }
        });

        builder.Services.TryAddSingleton<IForgejoCommentService, ForgejoCommentService>();
        builder.Decorators.Replace<IForgejoCommentService, ForgejoDryRunCommentService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowResultSink, ForgejoJobSummaryResultSink>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowResultSink, ForgejoCommentResultSink>());
    }
}
