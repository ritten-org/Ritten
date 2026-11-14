namespace Ritten.NSchema;

/// <summary>
/// The result of a plan.
/// </summary>
/// <param name="HasChanges">Whether the project and the recorded state differ.</param>
/// <param name="Output">The plan, as markdown.</param>
public sealed record NSchemaPlanResult(bool HasChanges, string Output);
