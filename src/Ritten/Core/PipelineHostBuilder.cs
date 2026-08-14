using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Core.FileSystem;
using Ritten.Core.Runner;
using Ritten.Reporting;

namespace Ritten.Core;

/// <summary>
/// Provides functionality to configure and build a pipeline application.
/// </summary>
public class PipelineHostBuilder : IPipelineBuilder
{
    private readonly SpectreProgressReporter _reporter;
    private readonly string _pipelineName;
    private readonly Dictionary<string, Action<IJobBuilder>> _jobs = [];

    /// <summary>
    /// Creates a new instance of the <see cref="PipelineHostBuilder"/>.
    /// </summary>
    /// <param name="project">The project being built.</param>
    /// <param name="pipelineName">The name of the pipeline being configured.</param>
    /// <param name="reporter">The reporter that renders pipeline progress.</param>
    internal PipelineHostBuilder(RittenProject project, string pipelineName, SpectreProgressReporter reporter)
    {
        _reporter = reporter;
        _pipelineName = pipelineName;
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

        Services.AddSingleton(new PipelineJob(_pipelineName, job));
        Services.AddSingleton(TimeProvider.System);

        Services.TryAddSingleton<IPipelineRunner, DefaultPipelineRunner>();

        Services.TryAddSingleton<IFileSystem, ProjectFileSystem>();
        Services.TryAddSingleton<IPipelineState, DefaultPipelineState>();

        Services.AddSingleton<IProgressReporter>(_reporter);
        Services.TryAddSingleton<IPipelineLog>(_reporter);

        var builder = new JobBuilder(Services);
        configure(builder);

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
