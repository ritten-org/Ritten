using Ritten.Contracts.FileSystem;

namespace Ritten.Docker;

/// <summary>
/// A host directory bound into a container.
/// </summary>
/// <param name="Host">The directory on the host.</param>
/// <param name="Container">Where it appears inside the container.</param>
/// <param name="ReadOnly">Whether the container may write to it.</param>
public sealed record BindMount(IDirectory Host, string Container, bool ReadOnly = false);
