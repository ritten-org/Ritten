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
/// A self-describing pipeline.
/// </summary>
public abstract class Pipeline<TSettings> : Pipeline
{
    /// <summary>
    /// Validates the given settings against this pipeline's requirements.
    /// </summary>
    public virtual bool TryValidate(TSettings settings, out List<string> errors)
    {
        errors = [];
        return true;
    }

    /// <summary>
    /// Configures the services and steps for this pipeline.
    /// </summary>
    /// <param name="builder">The builder used to register services and declare steps.</param>
    /// <param name="settings">The settings read from the project's <c>ritten.json</c>.</param>
    public abstract void Configure(IPipelineBuilder builder, TSettings settings);
}
