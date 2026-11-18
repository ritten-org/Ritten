namespace Ritten.Bitwarden;

/// <summary>
/// An item as <c>bw list items</c> prints it, decrypted.
/// </summary>
/// <param name="Id">The item's id.</param>
/// <param name="Name">The item's name.</param>
/// <param name="CollectionIds">The collections the item is in.</param>
/// <param name="Notes">The item's notes.</param>
/// <param name="Login">The item's username and password, when it is a login.</param>
/// <param name="Fields">The item's custom fields.</param>
internal sealed record BitwardenItem(
    string Id,
    string Name,
    IReadOnlyList<string>? CollectionIds,
    string? Notes,
    BitwardenLogin? Login,
    IReadOnlyList<BitwardenField>? Fields)
{
    /// <summary>
    /// The value of a field: <c>username</c>, <c>password</c> and <c>notes</c> are the item's own, anything else
    /// a custom field of that name, ignoring case as 1Password's references do.
    /// </summary>
    /// <returns>The value; null when the item has no such field or it is empty.</returns>
    /// <exception cref="InvalidOperationException">Two custom fields have the name.</exception>
    public string? Read(string field)
    {
        var own = field.ToLowerInvariant() switch
        {
            "username" => Login?.Username,
            "password" => Login?.Password,
            "notes" => Notes,
            _ => null,
        };
        if (own is { Length: > 0 })
        {
            return own;
        }

        var named = (Fields ?? []).Where(candidate => string.Equals(candidate.Name, field, StringComparison.OrdinalIgnoreCase)).ToList();
        return named.Count switch
        {
            0 => null,
            1 => named[0].Value is { Length: > 0 } value ? value : null,
            _ => throw new InvalidOperationException($"'{Name}' has {named.Count} fields named '{field}'."),
        };
    }

    /// <summary>
    /// The names a field can be read by, for saying what there is when the one asked for is not.
    /// </summary>
    public IEnumerable<string> FieldNames()
    {
        if (Login?.Username is { Length: > 0 })
        {
            yield return "username";
        }

        if (Login?.Password is { Length: > 0 })
        {
            yield return "password";
        }

        if (Notes is { Length: > 0 })
        {
            yield return "notes";
        }

        foreach (var field in Fields ?? [])
        {
            yield return field.Name;
        }
    }
}
