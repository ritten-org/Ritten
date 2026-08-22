using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Engine.DryRun;
using Ritten.Engine.FileSystem;
using Ritten.Engine.Runtimes;
using Ritten.Engine.Workflows;
using Ritten.Reporting;
using Spectre.Console;

namespace Ritten.Engine.Runs;

/// <summary>
/// Assembles a declared <see cref="IJob"/> into a runnable <see cref="WorkflowRun"/> for one project.
/// </summary>
public class WorkflowRunBuilder : IWorkflowBuilder
{
    private readonly RittenProject _project;
    private readonly DetectRuntimeResult _runtime;
    private SelectedWorkflow? _workflow;
    private bool _dryRun;
    private bool _autoApprove;
    private JobArguments _arguments = JobArguments.None;
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
        Services.TryAddSingleton<IProjectFiles, ProjectFileClient>();
        Decorators.Decorate<IProjectFiles, DryRunProjectFiles>();
        Services.AddSingleton<IWorkflowProgress>(console);
        Services.TryAddSingleton<IWorkflowPrompt>(_ => new ConsolePrompt(AnsiConsole.Console));
    }

    /// <summary>
    /// The service collection the job is assembled into.
    /// </summary>
    public IServiceCollection Services { get; } = new ServiceCollection();

    /// <summary>
    /// The decorators applied during a dry run, for the run's services.
    /// </summary>
    public DecoratorRegistry Decorators { get; } = new();

    /// <summary>
    /// Sets the workflow the job belongs to.
    /// </summary>
    /// <param name="workflow">The workflow being assembled.</param>
    public WorkflowRunBuilder WithWorkflow(SelectedWorkflow workflow)
    {
        _workflow = workflow;
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
    /// Supplies the values read for the inputs the job declared.
    /// </summary>
    /// <param name="arguments">The values the caller gave, already read into their declared types.</param>
    public WorkflowRunBuilder WithArguments(JobArguments arguments)
    {
        _arguments = arguments;
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
        Services.AddSingleton(_workflow ?? throw new InvalidOperationException("The run has no workflow; call WithWorkflow first."));
        Services.AddSingleton(new WorkflowJob(_workflow.Workflow.Label, job.Name, _dryRun, _autoApprove));
        _runtime.Runtime.Configure(this, _runtime.Raw);
        job.Configure(this, settings.Value, _arguments);

        // Capability defaults land after the runtime's and the job's registrations: runtimes
        // declare theirs with TryAdd so an explicit host choice preempts them, and the engine
        // answers last of all, only for runs where nobody knows better.
        Services.TryAddSingleton(new RunContext());
        Services.TryAddSingleton(new PullRequest());
        Services.TryAddSingleton<IPullRequestLabels, NoPullRequestLabels>();

        Services.AddSingleton(job.Steps);
        foreach (var step in job.Steps)
        {
            Services.AddTransient(step.StepType);
        }

        if (_dryRun)
        {
            // These stub mappings let us decorate or outright replace services so they can't accidentally do anything irreversible.
            var decorators = Decorators.GetAll()
                .GroupBy(p => p.ServiceType)
                .Select(g => g.Last())
                .ToList();

            foreach (var decorator in decorators)
            {
                decorator.Decorate(Services);
            }
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
    /// Adds decorators that modify the services used by jobs.
    /// </summary>
    /// <param name="decorators">The shared decorators to adopt.</param>
    public WorkflowRunBuilder WithDecorators(DecoratorRegistry decorators)
    {
        foreach (var decorator in decorators.GetAll())
        {
            Decorators.Add(decorator);
        }

        return this;
    }
}
