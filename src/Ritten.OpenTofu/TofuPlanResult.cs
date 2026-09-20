namespace Ritten.OpenTofu;

/// <summary>
/// What a plan found.
/// </summary>
/// <param name="HasChanges">Whether applying this plan would change anything.</param>
/// <param name="Output">The plan as OpenTofu printed it.</param>
public sealed record TofuPlanResult(bool HasChanges, string Output);
