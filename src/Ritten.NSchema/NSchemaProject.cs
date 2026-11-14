using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ritten.NSchema;

/// <summary>
/// The NSchema project a command runs against: the directory holding its configuration
/// (<c>config.sql</c>), its schema and its <c>nschema.lock</c>.
/// </summary>
/// <param name="Directory">The project's directory, relative to the working directory or absolute, or null for the working directory itself.</param>
[JsonConverter(typeof(Converter))]
public sealed record NSchemaProject(string? Directory)
{
    /// <summary>
    /// The working directory itself, as the project.
    /// </summary>
    public static NSchemaProject CurrentDirectory { get; } = new((string?)null);

    /// <summary>
    /// Reads the project from its directory alone.
    /// </summary>
    private sealed class Converter : JsonConverter<NSchemaProject>
    {
        public override NSchemaProject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(reader.GetString());

        public override void Write(Utf8JsonWriter writer, NSchemaProject value, JsonSerializerOptions options) => writer.WriteStringValue(value.Directory);
    }
}
