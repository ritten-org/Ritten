namespace Ritten.Bitwarden;

/// <summary>
/// A collection as <c>bw list collections</c> prints it, its name decrypted.
/// </summary>
/// <param name="Id">The collection's id, which items name it by.</param>
/// <param name="Name">The collection's name.</param>
internal sealed record BitwardenCollection(string Id, string Name);
