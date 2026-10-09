using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ritten.DotNet;

/// <summary>
/// The repository a project declares it lives in, which wins over anything derived from its project files or remote.
/// </summary>
/// <param name="Url">The repository's web URL.</param>
[JsonConverter(typeof(Converter))]
public sealed record PackageRepository(string Url)
{
    /// <summary>
    /// Reads the repository from its URL alone.
    /// </summary>
    private sealed class Converter : JsonConverter<PackageRepository>
    {
        public override PackageRepository? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.GetString() is { Length: > 0 } url ? new PackageRepository(url) : null;

        public override void Write(Utf8JsonWriter writer, PackageRepository value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Url);
    }
}
