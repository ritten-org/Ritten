namespace Ritten.Bitwarden;

/// <summary>
/// Everything one user can see, decrypted, read once per run: every reference after the first costs nothing.
/// </summary>
/// <param name="Collections">The collections shared with the user.</param>
/// <param name="Items">The items the user can see.</param>
internal sealed record BitwardenVault(IReadOnlyList<BitwardenCollection> Collections, IReadOnlyList<BitwardenItem> Items)
{
    /// <summary>
    /// The value a reference names.
    /// </summary>
    /// <exception cref="InvalidOperationException">The collection, item or field is not there, or not just one.</exception>
    public string Read(BitwardenReference reference)
    {
        // Names, exactly: `bw get item` searches, so a name that is part of another's (beszel, beszel-agent)
        // answers with both. A reference names one item and is refused rather than guessed at.
        var collection = One(
            Collections.Where(candidate => candidate.Name == reference.Collection),
            reference,
            $"no collection named '{reference.Collection}' is shared with this user",
            count => $"{count} collections are named '{reference.Collection}'");

        var item = One(
            Items.Where(candidate => candidate.Name == reference.Item && (candidate.CollectionIds ?? []).Contains(collection.Id)),
            reference,
            $"'{reference.Collection}' has no item named '{reference.Item}'",
            count => $"'{reference.Collection}' has {count} items named '{reference.Item}', which a reference cannot tell apart: rename one");

        return item.Read(reference.Field) ?? throw new InvalidOperationException(
            $"{reference}: '{reference.Item}' has no '{reference.Field}'; it has {string.Join(", ", item.FieldNames().Select(name => $"'{name}'"))}.");
    }

    private static T One<T>(IEnumerable<T> candidates, BitwardenReference reference, string none, Func<int, string> many)
    {
        var found = candidates.ToList();
        return found.Count switch
        {
            1 => found[0],
            0 => throw new InvalidOperationException($"{reference}: {none}."),
            _ => throw new InvalidOperationException($"{reference}: {many(found.Count)}."),
        };
    }
}
