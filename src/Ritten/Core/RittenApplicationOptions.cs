namespace Ritten.Core;

/// <summary>
/// Settings for constructing a <see cref="RittenApplicationBuilder"/>.
/// </summary>
public class RittenApplicationOptions
{
    /// <summary>
    /// Gets or sets the command-line arguments.
    /// </summary>
    public string[]? Args { get; init; }

    /// <summary>
    /// Gets or sets the environment name.
    /// </summary>
    public string? EnvironmentName { get; init; }

    /// <summary>
    /// Gets or sets the application name.
    /// </summary>
    public string? ApplicationName { get; init; }

    /// <summary>
    /// Gets or sets the content root path.
    /// </summary>
    public string? ContentRootPath { get; init; }
}
