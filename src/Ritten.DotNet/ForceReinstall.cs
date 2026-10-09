using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ritten.DotNet;

/// <summary>
/// Whether an install should replace a tool that already carries the version being installed.
/// </summary>
/// <param name="Requested">Whether the caller asked for the reinstall.</param>
[JsonConverter(typeof(Converter))]
public sealed record ForceReinstall(bool Requested)
{
    /// <summary>
    /// The command-line flag that asks for a reinstall.
    /// </summary>
    public const string OptionName = "force";

    /// <summary>
    /// Reads the flag that asks for a reinstall.
    /// </summary>
    private sealed class Converter : JsonConverter<ForceReinstall>
    {
        public override ForceReinstall Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetBoolean());

        public override void Write(Utf8JsonWriter writer, ForceReinstall value, JsonSerializerOptions options) =>
            writer.WriteBooleanValue(value.Requested);
    }
}
