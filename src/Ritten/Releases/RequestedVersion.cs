using System.Text.Json;
using System.Text.Json.Serialization;
using NuGet.Versioning;

namespace Ritten.Releases;

/// <summary>
/// The version the caller named, rather than one derived from what the repository says.
/// </summary>
/// <param name="Version">The version asked for, or null when the caller named none.</param>
[JsonConverter(typeof(Converter))]
public sealed record RequestedVersion(NuGetVersion? Version)
{
    /// <summary>
    /// The command-line option that names the version.
    /// </summary>
    public const string OptionName = "version";

    /// <summary>
    /// The caller named no version.
    /// </summary>
    public static RequestedVersion None { get; } = new((NuGetVersion?)null);

    /// <summary>
    /// Reads the version from its text, refusing anything that isn't one in the release's own words.
    /// </summary>
    private sealed class Converter : JsonConverter<RequestedVersion>
    {
        public override RequestedVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = reader.GetString();
            return NuGetVersion.TryParse(text, out var version)
                ? new RequestedVersion(version)
                : throw new JsonException($"'{text}' is not a version. Give one like 1.2.0.");
        }

        public override void Write(Utf8JsonWriter writer, RequestedVersion value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Version?.ToNormalizedString());
    }
}
