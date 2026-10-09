using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Engine.DryRun;
using Ritten.Engine.FileSystem;
using Ritten.Engine.Runs;
using Ritten.Engine.Runtimes;
using Ritten.Engine.Workflows;
using Ritten.Reporting;
using Spectre.Console;

namespace Ritten.Engine;

/// <summary>
/// Configures a <see cref="WorkflowApplication"/>.
/// </summary>
public sealed class WorkflowApplicationBuilder : IWorkflowBuilder
{
    internal WorkflowApplicationBuilder()
    {
    }

    /// <summary>
    /// The name of the file that marks a project's root and declares its workflow.
    /// </summary>
    public string ProjectFileName { get; set; } = RittenProject.DefaultFileName;

    /// <summary>
    /// The workflows the application can run.
    /// </summary>
    public WorkflowRegistry Workflows { get; } = new();

    /// <summary>
    /// The runtimes the application can find itself running in.
    /// </summary>
    public RuntimeRegistry Runtimes { get; } = new();

    /// <summary>
    /// Every service the application's jobs use.
    /// </summary>
    public IServiceCollection Services { get; } = new ServiceCollection();

    /// <summary>
    /// The decorators applied during a dry run.
    /// </summary>
    public DecoratorRegistry Decorators { get; } = new();

    /// <summary>
    /// Builds and validates the workflow application.
    /// </summary>
    public Result<WorkflowApplication> Build() => Build(Environment.GetEnvironmentVariable);

    /// <summary>
    /// Builds and validates the workflow application in the given environment.
    /// </summary>
    /// <param name="environment">The environment the runtime is detected from.</param>
    internal Result<WorkflowApplication> Build(Func<string, string?> environment)
    {
        List<Error> runtimeErrors = [.. Runtimes.Validate()];
        var runtime = runtimeErrors.Count == 0 ? Runtimes.Detect(environment) : null;
        if (runtime is not { IsSuccess: true })
        {
            return Fail([.. runtimeErrors, .. runtime?.Errors ?? []]);
        }

        var services = new ServiceCollection();
        var decorators = new DecoratorRegistry().Decorate<IProjectFiles, DryRunProjectFiles>();
        AddRunFacts(services, runtime.Value);
        foreach (var service in Services)
        {
            services.Add(service);
        }

        foreach (var decorator in Decorators.GetAll())
        {
            decorators.Add(decorator);
        }

        Workflows.Register(services);

        // The runtime's registrations land after the host's, so the more specific wins.
        runtime.Value.Runtime.Configure(new Composition(services, decorators), runtime.Value.Raw);
        AddDefaults(services);
        decorators.Apply(services);

        ServiceProvider provider;
        try
        {
            provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        }
        catch (AggregateException exception)
        {
            return Fail([.. exception.InnerExceptions.Select(e => Result.Error(e.Message, e))]);
        }

        var workflows = new WorkflowSet([.. provider.GetServices<IWorkflow>()]);
        List<Error> model = [.. workflows.Validate([.. provider.GetServices<IJobRule>()]), .. MissingStepServices(provider, workflows)];
        if (model.Count > 0)
        {
            provider.Dispose();
            return Fail(model);
        }

        return new WorkflowApplication(workflows, provider, runtime.Value, ProjectFileName);
    }

    private static Result<WorkflowApplication> Fail(List<Error> errors)
    {
        WorkflowApplication.EngineConsole().Errors(errors);
        return errors;
    }

    /// <summary>
    /// What every run is about, read from the scope's <see cref="RunState"/>.
    /// </summary>
    private static void AddRunFacts(IServiceCollection services, DetectRuntimeResult runtime)
    {
        services.AddOptions();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(new WorkflowEnvironment(runtime.Environment));
        services.AddScoped<RunState>();
        services.AddScoped(provider => Current(provider).Workflow);
        services.AddScoped(provider => Current(provider).Workflow.Project);
        services.AddScoped(provider => Current(provider).Job);
        services.AddScoped(provider => Current(provider).Job.Steps);
        services.AddScoped(provider => Current(provider).Console);
        services.AddScoped<IWorkflowProgress>(provider => provider.GetRequiredService<IWorkflowConsole>());
        services.AddScoped(provider =>
        {
            var run = Current(provider);
            return new WorkflowJob(run.Workflow.Workflow.Label, run.Job.Name, run.Options.DryRun, run.Options.AutoApprove);
        });
    }

    /// <summary>
    /// What the engine provides unless the host or runtime registered its own.
    /// </summary>
    private static void AddDefaults(IServiceCollection services)
    {
        services.TryAddScoped<IWorkflowLog>(provider => provider.GetRequiredService<IWorkflowConsole>());
        services.TryAddScoped<IWorkflowRunner, DefaultWorkflowRunner>();
        services.TryAddScoped<IFileSystem, ProjectFileSystem>();
        services.TryAddScoped<IProjectFiles, ProjectFileClient>();
        services.TryAddSingleton<IWorkflowPrompt>(_ => new ConsolePrompt(AnsiConsole.Console));
        services.TryAddSingleton(new RunContext());
        services.TryAddSingleton(new PullRequest());
        services.TryAddScoped<IPullRequestLabels, NoPullRequestLabels>();
        services.TryAddScoped<ISecretProvider, DefaultSecretProviderProvider>();
    }

    private static Run Current(IServiceProvider provider) => provider.GetRequiredService<RunState>().Current;

    /// <summary>
    /// The services a step's constructor asks for that nothing registers. Steps are built as each run reaches them,
    /// so this is the only place a missing client can be caught before a job starts.
    /// </summary>
    private static IEnumerable<Error> MissingStepServices(IServiceProvider provider, WorkflowSet workflows)
    {
        var registered = provider.GetRequiredService<IServiceProviderIsService>();
        var steps = workflows.Workflows
            .SelectMany(w => w.Jobs)
            .SelectMany(j => j.Steps)
            .DistinctBy(s => s.StepType);
        foreach (var step in steps)
        {
            if (step.StepType.GetConstructors(BindingFlags.Public | BindingFlags.Instance) is not [var constructor])
            {
                continue;
            }

            foreach (var parameter in constructor.GetParameters().Where(p => !p.HasDefaultValue && !registered.IsService(p.ParameterType)))
            {
                yield return Result.Error($"'{step.Name}' needs a {parameter.ParameterType.Name}, which no service registers.");
            }
        }
    }

    /// <summary>
    /// What a runtime configures: the application's services and dry-run pairings, and nothing else.
    /// </summary>
    private sealed class Composition(IServiceCollection services, DecoratorRegistry decorators) : IWorkflowBuilder
    {
        public IServiceCollection Services => services;

        public DecoratorRegistry Decorators => decorators;
    }
}
