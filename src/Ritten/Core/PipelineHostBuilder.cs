using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Core.FileSystem;
using Ritten.Core.Runner;
using Ritten.Core.Runtimes;
using Ritten.Git;
using Ritten.GitHub;
using Ritten.NuGet;
using Ritten.Reporting;
using Spectre.Console;

namespace Ritten.Core;

/// <summary>
/// Assembles a declared <see cref="IJob"/> into a runnable <see cref="PipelineHost"/> for one project.
/// </summary>
public class PipelineHostBuilder
{
    private readonly RittenProject _project;
    private readonly string _pipelineLabel;
    private readonly bool _dryRun;
    private readonly bool _autoApprove;
    private readonly Func<string, string?> _environment;
    private readonly IPipelineLog _log;
    private readonly RuntimeRegistry _runtimes;

    /// <summary>
    /// Creates a new instance of the <see cref="PipelineHostBuilder"/>.
    /// </summary>
    /// <param name="project">The project being built.</param>
    /// <param name="pipelineLabel">The human label of the pipeline being assembled.</param>
    /// <param name="reporter">The reporter that renders pipeline progress.</param>
    /// <param name="dryRun">Whether to wrap the clients that reach outside the working directory.</param>
    /// <param name="autoApprove">Whether a job that would stop and ask has been approved up front.</param>
    /// <param name="environment">Reads environment variables; the process environment when not given.</param>
    /// <param name="log">Where the builder writes; the reporter when not given.</param>
    /// <param name="runtimes">The runtimes the host can find itself running in; local-only when not given.</param>
    internal PipelineHostBuilder(RittenProject project, string pipelineLabel, SpectreProgressReporter reporter, bool dryRun = false, bool autoApprove = false, Func<string, string?>? environment = null, IPipelineLog? log = null, RuntimeRegistry? runtimes = null)
    {
        _project = project;
        _pipelineLabel = pipelineLabel;
        _dryRun = dryRun;
        _autoApprove = autoApprove;
        _environment = environment ?? Environment.GetEnvironmentVariable;
        _log = log ?? reporter;
        _runtimes = runtimes ?? new RuntimeRegistry();

        // Applies to every run.
        Services.AddOptions();
        Services.AddSingleton(project);
        Services.AddSingleton(TimeProvider.System);
        Services.TryAddSingleton<IPipelineRunner, DefaultPipelineRunner>();
        Services.TryAddSingleton<IFileSystem, ProjectFileSystem>();
        Services.AddSingleton<IProgressReporter>(reporter);
        Services.TryAddSingleton(_log);
        Services.TryAddSingleton<IPipelinePrompt>(_ => new ConsolePrompt(AnsiConsole.Console));

    }

    /// <summary>
    /// The service collection the job is assembled into.
    /// </summary>
    public IServiceCollection Services { get; } = new ServiceCollection();

    /// <summary>
    /// Builds the <see cref="PipelineHost" /> for the given job.
    /// </summary>
    /// <param name="job">The declared job to assemble.</param>
    /// <returns>The configured pipeline application.</returns>
    public Result<PipelineHost> Build(IJob job)
    {
        var runtime = _runtimes.Detect(_environment);
        if (runtime.IsError)
        {
            return runtime.Errors;
        }

        var environment = runtime.Value.Environment;
        var settings = job.ReadSettings(_project, environment, _dryRun, _log);
        if (settings.IsError)
        {
            return settings.Errors;
        }

        Services.AddSingleton(new PipelineEnvironment(environment));
        Services.AddSingleton(new PipelineJob(_pipelineLabel, job.Name, _dryRun, _autoApprove));
        runtime.Value.Runtime.ConfigureServices(Services, _environment);
        job.ConfigureServices(Services, settings.Value);

        Services.AddSingleton(job.Steps);
        foreach (var step in job.Steps)
        {
            Services.AddTransient(step.StepType);
        }

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

        var ruleErrors = provider.GetServices<IJobRule>().SelectMany(rule => rule.Check(job)).ToList();
        if (ruleErrors.Count > 0)
        {
            provider.Dispose();
            return ruleErrors;
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
