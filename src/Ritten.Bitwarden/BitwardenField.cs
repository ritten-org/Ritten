namespace Ritten.Bitwarden;

/// <summary>
/// An item's custom field.
/// </summary>
/// <param name="Name">The field's name.</param>
/// <param name="Value">The field's value.</param>
internal sealed record BitwardenField(string Name, string? Value);
