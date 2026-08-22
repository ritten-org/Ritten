namespace Ritten.Engine.Workflows;

/// <summary>
/// A workflow that has validated a directory as being compatible with itself.
/// </summary>
/// <param name="Workflow">The workflow that recognized the repository.</param>
/// <param name="Reason">Why it did, phrased to be read by whoever is being told what will happen.</param>
public sealed record CompatibleWorkflow(IWorkflow Workflow, string Reason);
