using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Engine.Rules;

namespace Ritten.Engine.Workflows;

/// <summary>
/// The workflows an application can run, found by the name a project's <c>ritten.json</c> declares.
/// </summary>
internal sealed class WorkflowSet(IReadOnlyList<IWorkflow> workflows)
{
    // Validating a job is done as a set of defined rules.
    // These make sure that the steps can run in the order they're declared.
    private static readonly IJobRule[] Rules = [
        new ProduceBeforeConsume(),
        new GateBeforePublish(),
        new CheckBeforePublish(),
        new DeployJobsPublish()
    ];

    /// <summary>
    /// The registered workflows, in registration order.
    /// </summary>
    public IReadOnlyList<IWorkflow> Workflows => workflows;

    /// <summary>
    /// The registered workflow names, in registration order.
    /// </summary>
    public IEnumerable<string> Names => workflows.Select(p => p.Name);

    /// <summary>
    /// Finds the workflow registered under the given name.
    /// </summary>
    public IWorkflow? Find(string name) => workflows
        .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Asks each workflow whether it recognizes the given repository.
    /// </summary>
    /// <param name="directory">The repository being set up.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<CompatibleWorkflow?> IsCompatible(IDirectory directory, CancellationToken cancellationToken = default)
    {
        foreach (var workflow in workflows)
        {
            if (await workflow.IsCompatible(directory, cancellationToken) is { Length: > 0 } reason)
            {
                return new CompatibleWorkflow(workflow, reason);
            }
        }

        return null;
    }

    /// <summary>
    /// Validates the entire registered workflow model.
    /// </summary>
    /// <param name="registered">The rules the application's services add to the engine's own.</param>
    public IReadOnlyList<Error> Validate(IEnumerable<IJobRule> registered)
    {
        List<Error> errors =
        [
            .. workflows
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => Result.Error($"Two workflows are registered under the name '{g.Key}'."))
        ];

        foreach (var workflow in workflows)
        {
            var jobs = workflow.Jobs;
            errors.AddRange(jobs
                .GroupBy(j => j.Name)
                .Where(g => g.Count() > 1)
                .Select(g => Result.Error($"The {workflow.Label} workflow declares two jobs named '{g.Key}'.")));

            foreach (var job in jobs)
            {
                errors.AddRange(ArgumentsModel.For(job.ArgumentsType).Validate().Select(e => Contextualize(workflow, job, e)));
                errors.AddRange(Rules
                    .Concat(registered)
                    .SelectMany(rule => rule.Check(job))
                    .Select(e => Contextualize(workflow, job, e)));
            }
        }

        return errors;
    }

    private static Error Contextualize(IWorkflow workflow, IJob job, Error error) =>
        Result.Error($"{workflow.Label} {job.Name}: {error.Message}", error.Cause);
}
