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
/// Assembles a declared <see cref="IJob"/> into a runnable <see cref="WorkflowRun"/> for one project.
/// </summary>
public class WorkflowRunBuilder
{
    private readonly RittenProject _project;
    private readonly DetectRuntimeResult _runtime;
    private string _workflowLabel = "";
    private bool _dryRun;
    private bool _autoApprove;
    private IWorkflowLog _log;

    /// <summary>
    /// Creates a new instance of the <see cref="WorkflowRunBuilder"/>.
    /// </summary>
    /// <param name="project">The project being built.</param>
    /// <param name="runtime">The detected runtime the job runs in.</param>
    /// <param name="console">The console narrative the run renders through.</param>
    internal WorkflowRunBuilder(RittenProject project, DetectRuntimeResult runtime, IWorkflowConsole console)
    {
        _project = project;
        _runtime = runtime;
        _log = console;

        // Applies to every run.
        Services.AddOptions();
        Services.AddSingleton(project);
        Services.AddSingleton(TimeProvider.System);
        Services.TryAddSingleton<IWorkflowRunner, DefaultWorkflowRunner>();
        Services.TryAddSingleton<IFileSystem, ProjectFileSystem>();
        Services.AddSingleton<IProgressReporter>(console);
        Services.TryAddSingleton<IWorkflowPrompt>(_ => new ConsolePrompt(AnsiConsole.Console));
    }

    /// <summary>
    /// The service collection the job is assembled into.
    /// </summary>
    public IServiceCollection Services { get; } = new ServiceCollection();

    /// <summary>
    /// Names the workflow the job belongs to, for the run's narrative.
    /// </summary>
    /// <param name="label">The human label of the workflow being assembled.</param>
    public WorkflowRunBuilder WithWorkflowLabel(string label)
    {
        _workflowLabel = label;
        return this;
    }

    /// <summary>
    /// Rehearses the job: the clients that reach outside the working directory are wrapped, so
    /// nothing irreversible can happen no matter what the steps do.
    /// </summary>
    /// <param name="dryRun">Whether the run is a rehearsal.</param>
    public WorkflowRunBuilder WithDryRun(bool dryRun = true)
    {
        _dryRun = dryRun;
        return this;
    }

    /// <summary>
    /// Approves the job up front, for runs with nobody there to confirm.
    /// </summary>
    /// <param name="autoApprove">Whether a job that would stop and ask is pre-approved.</param>
    public WorkflowRunBuilder WithAutoApprove(bool autoApprove = true)
    {
        _autoApprove = autoApprove;
        return this;
    }

    /// <summary>
    /// Adds registrations shared by every job.
    /// </summary>
    /// <param name="services">The shared registrations to copy in.</param>
    public WorkflowRunBuilder WithServices(IEnumerable<ServiceDescriptor> services)
    {
        foreach (var service in services)
        {
            Services.Add(service);
        }

        return this;
    }

    /// <summary>
    /// Redirects what the builder itself writes — settings warnings and the like — away from the
    /// console.
    /// </summary>
    /// <param name="log">Where the builder writes.</param>
    internal WorkflowRunBuilder WithLog(IWorkflowLog log)
    {
        _log = log;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="WorkflowRun" /> for the given job.
    /// </summary>
    /// <param name="job">The declared job to assemble.</param>
    /// <returns>The configured workflow application.</returns>
    public Result<WorkflowRun> Build(IJob job)
    {
        // Registered here rather than at construction so a WithLog still lands; TryAdd, so a log
        // the host registered directly wins over the builder's own.
        Services.TryAddSingleton(_log);

        var settings = job.ReadSettings(_project, _runtime.Environment, _dryRun, _log);
        if (settings.IsError)
        {
            return settings.Errors;
        }

        Services.AddSingleton(new WorkflowEnvironment(_runtime.Environment));
        Services.AddSingleton(new WorkflowJob(_workflowLabel, job.Name, _dryRun, _autoApprove));
        _runtime.Runtime.ConfigureServices(Services, _runtime.Raw);
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

        return new WorkflowRun(provider);
    }

    /// <summary>
    /// Replaces a registered service with a decorator that wraps it. Does nothing when the service
    /// isn't registered, since a workflow only registers the capabilities it uses.
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
