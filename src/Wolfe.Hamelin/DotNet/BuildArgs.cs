namespace Wolfe.Hamelin.DotNet;

/// <summary>
/// Settings for a <see cref="IDotNet.Build"/> invocation.
/// </summary>
public record BuildArgs
{
    /// <summary>
    /// The project or solution to build; when null, whatever the current directory resolves to.
    /// </summary>
    public string? Project { get; init; }

    /// <summary>
    /// The build configuration (e.g. <c>Release</c>); the SDK default when null.
    /// </summary>
    public string? Configuration { get; init; }

    /// <summary>
    /// Skips the implicit restore, for pipelines with an explicit restore step.
    /// </summary>
    public bool NoRestore { get; init; }
}
