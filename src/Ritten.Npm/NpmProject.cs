using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ritten.Npm;

/// <summary>
/// The npm project a command runs in: the directory holding its <c>package.json</c> and
/// <c>package-lock.json</c>.
/// </summary>
/// <param name="Directory">The project's directory, relative to the working directory or absolute, or null for the working directory itself.</param>
[JsonConverter(typeof(Converter))]
public sealed record NpmProject(string? Directory)
{
    /// <summary>
    /// The working directory itself, as the project.
    /// </summary>
    public static NpmProject CurrentDirectory { get; } = new((string?)null);

    /// <summary>
    /// Reads the project from its directory alone.
    /// </summary>
    private sealed class Converter : JsonConverter<NpmProject>
    {
        public override NpmProject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(reader.GetString());

        public override void Write(Utf8JsonWriter writer, NpmProject value, JsonSerializerOptions options) => writer.WriteStringValue(value.Directory);
    }
}
