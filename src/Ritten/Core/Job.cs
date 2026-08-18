using Microsoft.Extensions.DependencyInjection;
using Ritten.Contracts;

namespace Ritten.Core;

/// <summary>
/// The base for declaring a job.
/// </summary>
/// <typeparam name="TSettings">The settings type the job's requirements and services read.</typeparam>
public abstract class Job<TSettings> : IJob where TSettings : WorkflowSettings
{
    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<Step> Steps { get; }

    /// <summary>
    /// Registers the services the job's steps need, deferred until a project's settings exist to
    /// configure them. Only the job being run registers anything, so jobs pay for exactly the
    /// services they declare.
    /// </summary>
    /// <param name="services">The service collection the job is assembled into.</param>
    /// <param name="settings">The project's parsed settings.</param>
    protected virtual void ConfigureServices(IServiceCollection services, TSettings settings)
    {
    }

    /// <summary>
    /// Judges the inputs the run arrived with, e.g. <c>settings.Require(s =&gt; s.Build.Project)</c>.
    /// Runs as part of the settings load, before anything is assembled.
    /// </summary>
    /// <param name="settings">The validator, holding the parsed settings and the environment.</param>
    protected virtual void ValidateSettings(SettingsValidator<TSettings> settings)
    {
    }

    Result<WorkflowSettings> IJob.ReadSettings(RittenProject project, Func<string, string?> environment, bool dryRun, IWorkflowLog log)
    {
        var settings = WorkflowSettings.Read<TSettings>(project);
        if (settings.IsError)
        {
            return new Result<WorkflowSettings>(settings.Errors);
        }

        var validator = new SettingsValidator<TSettings>(settings.Value, environment, dryRun, log);
        ValidateSettings(validator);
        return validator.Errors.Count > 0
            ? new Result<WorkflowSettings>(validator.Errors)
            : new Result<WorkflowSettings>(settings.Value);
    }

    void IJob.ConfigureServices(IServiceCollection services, WorkflowSettings settings) =>
        ConfigureServices(services, (TSettings)settings);
}
