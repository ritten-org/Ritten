using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ritten.OpenTofu;

/// <summary>
/// The root module OpenTofu runs against.
/// </summary>
/// <param name="Root">The root module's directory, relative to the project, or null for the project itself.</param>
[JsonConverter(typeof(Converter))]
public sealed record TofuModule(string? Root)
{
    /// <summary>
    /// The project itself, as the root module.
    /// </summary>
    public static TofuModule Project { get; } = new((string?)null);

    /// <summary>
    /// Reads the module from its directory alone.
    /// </summary>
    private sealed class Converter : JsonConverter<TofuModule>
    {
        public override TofuModule Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(reader.GetString());

        public override void Write(Utf8JsonWriter writer, TofuModule value, JsonSerializerOptions options) => writer.WriteStringValue(value.Root);
    }
}
