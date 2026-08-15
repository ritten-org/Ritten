using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Core.FileSystem;
using Ritten.Core.Rules;
using Ritten.Core.Runner;
using Ritten.Git;
using Ritten.GitHub;
using Ritten.NuGet;
using Ritten.Reporting;
using Spectre.Console;

namespace Ritten.Core;

/// <summary>
/// Provides functionality to configure and build a pipeline application.
/// </summary>
public class PipelineHostBuilder : IPipelineBuilder
{
    private readonly string _pipelineName;
    private readonly Dictionary<string, Action<IJobBuilder>> _jobs = [];
    private readonly bool _dryRun;
    private readonly bool _autoApprove;
    private readonly Func<string, string?> _environment;
    private readonly IPipelineLog _log;

    /// <summary>
    /// Creates a new instance of the <see cref="PipelineHostBuilder"/>.
    /// </summary>
    /// <param name="project">The project being built.</param>
    /// <param name="pipelineName">The name of the pipeline being configured.</param>
    /// <param name="reporter">The reporter that renders pipeline progress.</param>
    /// <param name="dryRun">Whether to wrap the clients that reach outside the working directory.</param>
    /// <param name="autoApprove">Whether a job that would stop and ask has been approved up front.</param>
    /// <param name="environment">Reads environment variables; the process environment when not given.</param>
    /// <param name="log">Where the builder writes; the reporter when not given.</param>
    internal PipelineHostBuilder(RittenProject project, string pipelineName, SpectreProgressReporter reporter, bool dryRun = false, bool autoApprove = false, Func<string, string?>? environment = null, IPipelineLog? log = null)
    {
        _pipelineName = pipelineName;
        _dryRun = dryRun;
        _autoApprove = autoApprove;
        _environment = environment ?? Environment.GetEnvironmentVariable;
        _log = log ?? reporter;

        // Applies to every run.
        Services.AddSingleton(project);
        Services.AddSingleton(TimeProvider.System);
        Services.TryAddSingleton<IPipelineRunner, DefaultPipelineRunner>();
        Services.TryAddSingleton<IFileSystem, ProjectFileSystem>();
        Services.AddSingleton<IProgressReporter>(reporter);
        Services.TryAddSingleton(_log);
        Services.TryAddSingleton<IPipelinePrompt>(_ => new ConsolePrompt(AnsiConsole.Console));

        // The invariants every job shape must hold; pipelines can register more.
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<IJobRule, ProduceBeforeConsume>());
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<IJobRule, GateBeforePublish>());
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<IJobRule, ValidationBeforePublish>());
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

        var builder = new JobBuilder(Services, _log, _environment, _dryRun);
        configure(builder);
        var result = builder.Build();
        if (result.IsError)
        {
            return result.Errors;
        }
        Services.AddSingleton(result.Value);

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

        var provider = Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        var steps = result.Value.Select(descriptor => descriptor.Step).ToList();
        var errors = provider.GetServices<IJobRule>().SelectMany(rule => rule.Check(steps)).ToList();
        if (errors.Count > 0)
        {
            provider.Dispose();
            return errors;
        }

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
}
