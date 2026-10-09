using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ritten.Docker;

/// <summary>
/// The images a component builds from its own source.
/// </summary>
/// <param name="Images">Each image, by the tag the compose file refers to it by.</param>
[JsonConverter(typeof(Converter))]
public sealed record DockerImages(IReadOnlyList<DockerImage> Images)
{
    /// <summary>
    /// No images, for a component that only runs ones built elsewhere.
    /// </summary>
    public static DockerImages None { get; } = new([]);

    /// <summary>
    /// Reads the images from a plain list.
    /// </summary>
    private sealed class Converter : JsonConverter<DockerImages>
    {
        public override DockerImages Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(JsonSerializer.Deserialize<IReadOnlyList<DockerImage>>(ref reader, options) ?? []);

        public override void Write(Utf8JsonWriter writer, DockerImages value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value.Images, options);
    }
}
