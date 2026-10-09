namespace Ritten.Engine.Workflows;

/// <summary>
/// The arguments of a job that needs none.
/// </summary>
public sealed record NoArguments
{
    /// <summary>
    /// The only value there is.
    /// </summary>
    public static NoArguments Instance { get; } = new();
}