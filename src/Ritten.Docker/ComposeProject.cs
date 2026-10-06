using System.Text.Json;
using System.Text.Json.Serialization;
using Ritten.Engine;

namespace Ritten.Docker;

/// <summary>
/// A compose project as compose resolves it: its files merged and its environment interpolated.
/// </summary>
/// <param name="Services">Every service, ordered by name.</param>
public sealed record ComposeProject(IReadOnlyList<ComposeService> Services)
{
    /// <summary>
    /// Reads the project from what <c>docker compose config --format json</c> prints.
    /// </summary>
    internal static Result<ComposeProject> Parse(string json)
    {
        Document? document;
        try
        {
            document = JsonSerializer.Deserialize<Document>(json);
        }
        catch (JsonException e)
        {
            return new Error($"compose printed something other than a project: {e.Message}");
        }

        return new ComposeProject([
            .. (document?.Services ?? [])
                .Select(service => service.Value.ToService(service.Key))
                .OrderBy(service => service.Name, StringComparer.Ordinal)
        ]);
    }

    private sealed record Document([property: JsonPropertyName("services")] Dictionary<string, Definition>? Services);

    private sealed record Definition(
        [property: JsonPropertyName("labels")] Dictionary<string, string>? Labels,
        [property: JsonPropertyName("environment")] Dictionary<string, string?>? Environment,
        [property: JsonPropertyName("ports")] List<Port>? Ports,
        [property: JsonPropertyName("network_mode")] string? NetworkMode,
        [property: JsonPropertyName("container_name")] string? ContainerName)
    {
        public ComposeService ToService(string name) =>
            new(name, Labels ?? [], Environment ?? [], [.. (Ports ?? []).Select(port => port.ToPort())], NetworkMode)
            {
                ContainerName = ContainerName
            };
    }

    // Compose prints a published port as a string, which is a range when the file gave one.
    private sealed record Port(
        [property: JsonPropertyName("target")] int Target,
        [property: JsonPropertyName("published")] string? Published,
        [property: JsonPropertyName("host_ip")] string? HostIp,
        [property: JsonPropertyName("protocol")] string? Protocol)
    {
        public ComposePort ToPort() =>
            new(Target, int.TryParse(Published, out var published) ? published : null, HostIp, Protocol ?? "tcp");
    }
}
