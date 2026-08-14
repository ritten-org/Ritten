namespace Ritten.Core;

/// <summary>
/// A self-describing pipeline.
/// </summary>
public abstract class Pipeline
{
    /// <summary>
    /// Gets the name of the pipeline.
    /// </summary>
    public abstract string Name { get; }
}

/// <summary>
/// A pipeline configured by <typeparamref name="TSettings"/>.
/// </summary>
public abstract class Pipeline<TSettings> : Pipeline where TSettings : class
{
    /// <summary>
    /// Declares the services and jobs for this pipeline.
    /// </summary>
    /// <param name="builder">The builder used to register services and declare jobs.</param>
    /// <param name="settings">The settings read from the project's <c>ritten.json</c>.</param>
    public abstract void Configure(IPipelineBuilder builder, TSettings settings);
}
