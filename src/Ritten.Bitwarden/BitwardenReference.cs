namespace Ritten.Bitwarden;

/// <summary>
/// A <c>bw://collection/item/field</c> reference: a field of the one item of that name in the collection.
/// </summary>
/// <param name="Collection">The collection's name.</param>
/// <param name="Item">The item's name, matched exactly.</param>
/// <param name="Field">The field's name: <c>username</c>, <c>password</c>, <c>notes</c>, or a custom field's.</param>
internal sealed record BitwardenReference(string Collection, string Item, string Field)
{
    internal const string Scheme = "bw://";

    /// <summary>
    /// Reads a value as a reference, or null for a value that is not one.
    /// </summary>
    /// <exception cref="InvalidOperationException">The value has the scheme but not the shape.</exception>
    public static BitwardenReference? Parse(string value)
    {
        if (!value.StartsWith(Scheme, StringComparison.Ordinal))
        {
            return null;
        }

        // The scheme makes it ours, so a malformed one is refused here rather than handed on as a literal that
        // happens to start with bw://.
        var parts = value[Scheme.Length..].Split('/');
        return parts.Length == 3 && !parts.Any(string.IsNullOrWhiteSpace)
            ? new BitwardenReference(parts[0], parts[1], parts[2])
            : throw new InvalidOperationException($"'{value}' must be {Scheme}<collection>/<item>/<field>.");
    }

    /// <inheritdoc />
    public override string ToString() => $"{Scheme}{Collection}/{Item}/{Field}";
}
