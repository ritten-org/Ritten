namespace Ritten.Core;

/// <summary>
/// A self-describing pipeline that declares both its service dependencies and its steps.
/// </summary>
public abstract class Pipeline
{
    /// <summary>
    /// Configures the services and steps for this pipeline.
    /// </summary>
    /// <param name="builder">The builder used to register services and declare steps.</param>
    public abstract void Configure(IPipelineBuilder builder);
}
