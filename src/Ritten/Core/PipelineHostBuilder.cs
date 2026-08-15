using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Core.FileSystem;
using Ritten.Core.Runner;
using Ritten.Git;
using Ritten.NuGet;
using Ritten.Reporting;
using Ritten.Runtimes;
using Ritten.Runtimes.GitHubActions;
using Spectre.Console;

namespace Ritten.Core;

/// <summary>
/// Provides functionality to configure and build a pipeline application.
/// </summary>
public class PipelineHostBuilder : IPipelineBuilder
{
    private readonly SpectreProgressReporter _reporter;
    private readonly string _pipelineName;
    private readonly Dictionary<string, Action<IJobBuilder>> _jobs = [];
    private readonly bool _dryRun;
    private readonly bool _autoApprove;

    /// <summary>
    /// Creates a new instance of the <see cref="PipelineHostBuilder"/>.
    /// </summary>
    /// <param name="project">The project being built.</param>
    /// <param name="pipelineName">The name of the pipeline being configured.</param>
    /// <param name="reporter">The reporter that renders pipeline progress.</param>
    /// <param name="dryRun">Whether to wrap the clients that reach outside the working directory.</param>
    /// <param name="autoApprove">Whether a job that would stop and ask has been approved up front.</param>
    internal PipelineHostBuilder(RittenProject project, string pipelineName, SpectreProgressReporter reporter, bool dryRun = false, bool autoApprove = false)
    {
        _reporter = reporter;
        _pipelineName = pipelineName;
        _dryRun = dryRun;
        _autoApprove = autoApprove;
        Services.AddSingleton(project);
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; } = new ServiceCollection();

    /// <inheritdoc />
    public IPipelineBuilder AddJob(string name, Action<IJobBuilder> configure)
    {
        _jobs.Add(name, configure);
        return this;
    }

    /// <summary>
    /// Builds the <see cref="PipelineHost" /> for the given job.
    /// </summary>
    /// <returns>The configured pipeline application.</returns>
    public Result<PipelineHost> Build(string job)
    {
        if (!_jobs.TryGetValue(job, out var configure))
        {
            return Result.Error($"The {_pipelineName} pipeline has no job named '{job}'.");
        }

        Services.AddSingleton(new PipelineJob(_pipelineName, job, _dryRun, _autoApprove));
        Services.AddSingleton(TimeProvider.System);

        Services.TryAddSingleton<IPipelineRunner, DefaultPipelineRunner>();

        Services.TryAddSingleton<IFileSystem, ProjectFileSystem>();
        Services.TryAddSingleton<IPipelineState, DefaultPipelineState>();

        Services.AddSingleton<IProgressReporter>(_reporter);
        Services.TryAddSingleton<IPipelineLog>(_reporter);
        Services.TryAddSingleton<IPipelinePrompt>(_ => new ConsolePrompt(AnsiConsole.Console));

        var builder = new JobBuilder(Services);
        configure(builder);

        if (_dryRun)
        {
            // Wrapping the clients rather than asking each step to check a flag: a step that
            // reaches outside the working directory can only do it through one of these, so a
            // step added later is covered without anyone remembering to handle it.
            Decorate<INuGet, DryRunNuGet>();
            Decorate<IGit, DryRunGit>();
            Replace<IReleaseService, DryRunReleaseService>();
            // Nothing on ICommentService reads, so its dry run substitutes rather than wraps.
            Replace<ICommentService, DryRunCommentService>();
        }

        if (builder.Errors.Count > 0)
        {
            return builder.Errors;
        }

        var provider = Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        return new PipelineHost(provider);
    }

    /// <summary>
    /// Replaces a registered service with a decorator that wraps it. Does nothing when the service
    /// isn't registered, since a pipeline only registers the capabilities it uses.
    /// </summary>
    private void Decorate<TService, TDecorator>()
        where TService : class
        where TDecorator : class, TService
    {
        if (Services.LastOrDefault(d => d.ServiceType == typeof(TService)) is not { } registration)
        {
            return;
        }

        Services.Remove(registration);
        Services.AddSingleton<TService>(provider =>
        {
            var inner = Resolve<TService>(provider, registration);
            return ActivatorUtilities.CreateInstance<TDecorator>(provider, inner);
        });
    }

    /// <summary>
    /// Replaces a registered service outright, for a stand-in that has no need of the real one.
    /// Does nothing when the service isn't registered.
    /// </summary>
    private void Replace<TService, TReplacement>()
        where TService : class
        where TReplacement : class, TService
    {
        if (Services.LastOrDefault(d => d.ServiceType == typeof(TService)) is not { } registration)
        {
            return;
        }

        Services.Remove(registration);
        Services.AddSingleton<TService, TReplacement>();
    }

    private static TService Resolve<TService>(IServiceProvider provider, ServiceDescriptor registration)
        where TService : class => registration switch
        {
            { ImplementationInstance: TService instance } => instance,
            { ImplementationFactory: { } factory } => (TService)factory(provider),
            { ImplementationType: { } type } => (TService)ActivatorUtilities.CreateInstance(provider, type),
            _ => throw new InvalidOperationException($"Cannot decorate {typeof(TService).Name}: it has no implementation.")
        };

    private sealed class JobBuilder(IServiceCollection services) : IJobBuilder
    {
        /// <summary>
        /// Turns <c>settings.Build.Project</c> into <c>build.project</c>.
        /// </summary>
        private static string SettingKey(string expression)
        {
            var segments = expression.Split('.');
            return string.Join('.', segments
                .Skip(segments.Length > 1 ? 1 : 0)
                .Select(JsonNamingPolicy.CamelCase.ConvertName));
        }

        public List<Error> Errors { get; } = [];

        public IJobBuilder Requires(string? value, string expression = "")
        {
            if (string.IsNullOrEmpty(value))
            {
                Errors.Add($"'{SettingKey(expression)}' not set in {RittenProject.FileName}.");
            }

            return this;
        }

        public IJobBuilder UseStep<TStep>() where TStep : class, IPipelineStep
        {
            services.AddTransient<IPipelineStep, TStep>();
            return this;
        }
    }
}
