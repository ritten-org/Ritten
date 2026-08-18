using Ritten.Contracts;
using Ritten.Core.Rules;

namespace Ritten.Core;

/// <summary>
/// The pipelines a host can run, found by the name a project's <c>ritten.json</c> declares.
/// </summary>
public sealed class PipelineRegistry
{
    // Validating a job is done as a set of defined rules.
    // These make sure that the steps can run in the order they're declared.
    private static readonly IJobRule[] Rules = [
        new ProduceBeforeConsume(),
        new GateBeforePublish(),
        new CheckBeforePublish()
    ];

    private readonly List<IPipeline> _pipelines = [];

    /// <summary>
    /// Registers a pipeline.
    /// </summary>
    /// <param name="pipeline">The pipeline to register.</param>
    public PipelineRegistry Add(IPipeline pipeline)
    {
        _pipelines.Add(pipeline);
        return this;
    }

    /// <summary>
    /// Registers a pipeline by type.
    /// </summary>
    /// <typeparam name="T">The pipeline type to construct and register.</typeparam>
    public PipelineRegistry Add<T>() where T : IPipeline, new() => Add(new T());

    /// <summary>
    /// The registered pipelines, in registration order.
    /// </summary>
    public IReadOnlyList<IPipeline> Pipelines => _pipelines;

    /// <summary>
    /// The registered pipeline names, in registration order.
    /// </summary>
    internal IEnumerable<string> Names => _pipelines.Select(p => p.Name);

    /// <summary>
    /// Finds the pipeline registered under the given name.
    /// </summary>
    internal IPipeline? Find(string name) => _pipelines
        .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Validates the entire registered pipeline model.
    /// </summary>
    internal IReadOnlyList<Error> Validate()
    {
        List<Error> errors =
        [
            .. _pipelines
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => Result.Error($"Two pipelines are registered under the name '{g.Key}'."))
        ];

        foreach (var pipeline in _pipelines)
        {
            var jobs = pipeline.Jobs;
            errors.AddRange(jobs
                .GroupBy(j => j.Name)
                .Where(g => g.Count() > 1)
                .Select(g => Result.Error($"The {pipeline.Label} pipeline declares two jobs named '{g.Key}'.")));

            foreach (var job in jobs)
            {
                errors.AddRange(Rules
                    .SelectMany(rule => rule.Check(job))
                    .Select(e => Contextualize(pipeline, job, e)));
            }
        }

        return errors;
    }

    private static Error Contextualize(IPipeline pipeline, IJob job, Error error) =>
        Result.Error($"{pipeline.Label} {job.Name}: {error.Message}", error.Cause);
}
