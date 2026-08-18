using Ritten.Contracts;
using Ritten.Engine.Rules;

namespace Ritten.Engine;

/// <summary>
/// The workflows a host can run, found by the name a project's <c>ritten.json</c> declares.
/// </summary>
public sealed class WorkflowRegistry
{
    // Validating a job is done as a set of defined rules.
    // These make sure that the steps can run in the order they're declared.
    private static readonly IJobRule[] Rules = [
        new ProduceBeforeConsume(),
        new GateBeforePublish(),
        new CheckBeforePublish()
    ];

    private readonly List<IWorkflow> _workflows = [];

    /// <summary>
    /// Registers a workflow.
    /// </summary>
    /// <param name="workflow">The workflow to register.</param>
    public WorkflowRegistry Add(IWorkflow workflow)
    {
        _workflows.Add(workflow);
        return this;
    }

    /// <summary>
    /// Registers a workflow by type.
    /// </summary>
    /// <typeparam name="T">The workflow type to construct and register.</typeparam>
    public WorkflowRegistry Add<T>() where T : IWorkflow, new() => Add(new T());

    /// <summary>
    /// The registered workflows, in registration order.
    /// </summary>
    public IReadOnlyList<IWorkflow> Workflows => _workflows;

    /// <summary>
    /// The registered workflow names, in registration order.
    /// </summary>
    internal IEnumerable<string> Names => _workflows.Select(p => p.Name);

    /// <summary>
    /// Finds the workflow registered under the given name.
    /// </summary>
    internal IWorkflow? Find(string name) => _workflows
        .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Validates the entire registered workflow model.
    /// </summary>
    internal IReadOnlyList<Error> Validate()
    {
        List<Error> errors =
        [
            .. _workflows
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => Result.Error($"Two workflows are registered under the name '{g.Key}'."))
        ];

        foreach (var workflow in _workflows)
        {
            var jobs = workflow.Jobs;
            errors.AddRange(jobs
                .GroupBy(j => j.Name)
                .Where(g => g.Count() > 1)
                .Select(g => Result.Error($"The {workflow.Label} workflow declares two jobs named '{g.Key}'.")));

            foreach (var job in jobs)
            {
                errors.AddRange(Rules
                    .SelectMany(rule => rule.Check(job))
                    .Select(e => Contextualize(workflow, job, e)));
            }
        }

        return errors;
    }

    private static Error Contextualize(IWorkflow workflow, IJob job, Error error) =>
        Result.Error($"{workflow.Label} {job.Name}: {error.Message}", error.Cause);
}
