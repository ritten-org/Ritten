using Ritten.Contracts.FileSystem;

namespace Ritten.OpenTofu;

/// <summary>
/// Describes an OpenTofu environment.
/// </summary>
public sealed record TofuEnvironment
{
    /// <summary>
    /// The variable file handed to plan and apply, or null for the root module's defaults.
    /// </summary>
    public IFile? VarFile { get; init; }

    /// <summary>
    /// The env files every command runs under, in order; a later file's entry replaces an earlier one's.
    /// </summary>
    public IReadOnlyList<IFile> EnvFiles { get; init; } = [];
}
