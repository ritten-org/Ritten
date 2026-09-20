namespace Ritten.OpenTofu;

/// <summary>
/// Which root module the OpenTofu client works on, and what it reads its variables from.
/// </summary>
public sealed class OpenTofuOptions
{
    /// <summary>
    /// The root module, relative to the project. Null when the project directory is the root,
    /// which is the usual case for a component.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// A variable file to pass to init, plan and apply, relative to the root module. Null when
    /// the root takes its variables from the environment, as a root with no <c>.tfvars</c> does.
    /// </summary>
    public string? VarFile { get; set; }
}
