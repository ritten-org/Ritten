namespace Ritten.OpenTofu;

/// <summary>
/// Options for the OpenTofu client.
/// </summary>
public sealed class OpenTofuOptions
{
    /// <summary>
    /// The root module, relative to the project, or null for the project itself.
    /// </summary>
    public string? Root { get; set; }
}
