using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ritten.Engine;

/// <summary>
/// A Ritten project file as a document rather than as settings.
/// </summary>
public sealed class ProjectFile
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    private readonly JsonObject _root;

    internal ProjectFile(JsonObject root) => _root = root;

    /// <summary>
    /// The default project file.
    /// </summary>
    public static ProjectFile Empty => new([]);

    /// <summary>
    /// The workflow the project declares, or null when it declares none.
    /// </summary>
    public string? Workflow
    {
        get => _root["workflow"]?.GetValue<string>();
        set
        {
            if (_root.ContainsKey("workflow"))
            {
                _root["workflow"] = value;
                return;
            }

            _root.Insert(0, "workflow", value);
        }
    }

    /// <summary>
    /// Whether the document already says something at the given key, e.g. <c>build.projects</c>.
    /// </summary>
    /// <param name="key">The dotted key path to look for.</param>
    public bool Has(string key) => Find(key) is not null;

    /// <summary>
    /// Sets the value at the given dotted key path, creating the objects along the way.
    /// </summary>
    /// <param name="key">The dotted key path to write, e.g. <c>build.project</c>.</param>
    /// <param name="value">The value to write.</param>
    public ProjectFile Set(string key, string value) => Set(key, JsonValue.Create(value));

    /// <summary>
    /// Sets the list at the given dotted key path, creating the objects along the way.
    /// </summary>
    /// <param name="key">The dotted key path to write, e.g. <c>build.projects</c>.</param>
    /// <param name="values">The values to write.</param>
    public ProjectFile Set(string key, IEnumerable<string> values) =>
        Set(key, new JsonArray([.. values.Select(v => (JsonNode)JsonValue.Create(v))]));

    /// <summary>
    /// Renders the document as it would be written.
    /// </summary>
    public override string ToString() => _root.ToJsonString(Indented) + "\n";

    private ProjectFile Set(string key, JsonNode? value)
    {
        var path = key.Split('.');
        var parent = _root;
        foreach (var segment in path[..^1])
        {
            if (parent[segment] is not JsonObject child)
            {
                child = [];
                parent[segment] = child;
            }

            parent = child;
        }

        parent[path[^1]] = value;
        return this;
    }

    private JsonNode? Find(string key)
    {
        JsonNode? node = _root;
        foreach (var segment in key.Split('.'))
        {
            if (node is not JsonObject parent)
            {
                return null;
            }

            node = parent[segment];
        }

        return node;
    }
}
