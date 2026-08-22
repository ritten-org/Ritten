using YamlDotNet.RepresentationModel;

namespace Ritten.GitHub;

/// <summary>
/// A GitHub Actions workflow file.
/// </summary>
public sealed class ActionsWorkflow
{
    private readonly string[] _lines;
    private readonly string _newline;

    private ActionsWorkflow(string text)
    {
        Text = text;
        _newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        _lines = [.. text.Split('\n').Select(line => line.TrimEnd('\r'))];

        var root = Root(text);
        Name = Scalar(root, "name");
        WorkingDirectory = WorkingDirectoryOf(root);
        Triggers = [.. Keys(Value(root, "on") as YamlMappingNode)];
        Jobs = [.. JobsOf(Value(root, "jobs") as YamlMappingNode)];
    }

    /// <summary>
    /// The document as it stands.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// The workflow's name, as the Actions tab lists it.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// The directory every run step defaults to, when the workflow sets one.
    /// </summary>
    public string? WorkingDirectory { get; }

    /// <summary>
    /// The events the workflow triggers on.
    /// </summary>
    public IReadOnlyCollection<string> Triggers { get; }

    /// <summary>
    /// The workflow's jobs, in the order they're declared.
    /// </summary>
    public IReadOnlyList<ActionsJob> Jobs { get; }

    /// <summary>
    /// Parses the given workflow document.
    /// </summary>
    /// <param name="text">The document to parse.</param>
    /// <exception cref="YamlDotNet.Core.YamlException">The document isn't YAML.</exception>
    public static ActionsWorkflow Parse(string text) => new(text);

    /// <summary>
    /// Returns the workflow with the given job saying what the given block says, replacing the
    /// job of that id when it has one and adding it at the end of the jobs when it hasn't.
    /// </summary>
    /// <param name="id">The id of the job to write.</param>
    /// <param name="block">The job as YAML, indented as it will appear under <c>jobs</c>.</param>
    public ActionsWorkflow WithJob(string id, string block) =>
        FindKey(id, under: "jobs") is { } key ? Replace(Block(key), block) : Append(block, under: "jobs", separate: true);

    /// <summary>
    /// Returns the workflow triggering on the given event, leaving the event's own configuration
    /// alone when it already triggers on it — a repository that narrowed its branches meant to.
    /// </summary>
    /// <param name="trigger">The event's name, e.g. <c>pull_request</c>.</param>
    /// <param name="block">The event as YAML, indented as it will appear under <c>on</c>.</param>
    public ActionsWorkflow WithTrigger(string trigger, string block) =>
        Triggers.Contains(trigger) ? this : Append(block, under: "on", separate: false);

    /// <inheritdoc />
    public override string ToString() => Text;

    /// <summary>
    /// Replaces the given span of lines with the given block.
    /// </summary>
    private ActionsWorkflow Replace((int Start, int End) span, string block)
    {
        List<string> lines = [.. _lines[..span.Start], .. Lines(block), .. _lines[span.End..]];
        return new ActionsWorkflow(string.Join(_newline, lines));
    }

    /// <summary>
    /// Adds the given block at the end of the given top-level mapping, or at the end of the
    /// document when the mapping isn't there at all. Jobs read better a blank line apart and
    /// triggers read better together, so the caller says which it is adding.
    /// </summary>
    private ActionsWorkflow Append(string block, string under, bool separate)
    {
        if (FindKey(under, under: null) is not { } key)
        {
            List<string> lines = [.. TrimEnd(_lines), "", $"{under}:", .. Lines(block)];
            return new ActionsWorkflow(string.Join(_newline, lines) + _newline);
        }

        var mapping = Block(key);
        var (from, to, _) = Entries(mapping);
        List<string> before = [.. TrimEnd(_lines[..mapping.End])];
        List<string> after = [.. _lines[mapping.End..]];

        // There's nothing to separate the first entry from.
        List<string> separator = separate && from < to ? [""] : [];
        return new ActionsWorkflow(string.Join(_newline, [.. before, .. separator, .. Lines(block), .. after]));
    }

    /// <summary>
    /// Where the given key's line is, and how deeply it's indented. A key is only ever looked for
    /// among its siblings — the entries of the document, or the entries of one of its mappings —
    /// so a job called <c>check</c> is never confused for a step of one.
    /// </summary>
    private (int Line, int Indent)? FindKey(string name, string? under)
    {
        var (from, to, depth) = under is null
            ? (0, _lines.Length, 0)
            : FindKey(under, under: null) is { } parent
                ? Entries(Block(parent))
                : (0, 0, 0);

        for (var line = from; line < to; line++)
        {
            if (Indent(_lines[line]) == depth && Names(_lines[line].TrimStart(), name))
            {
                return (line, depth);
            }
        }

        return null;
    }

    /// <summary>
    /// The lines a mapping's own entries live on, and the indentation they share. YAML lets a
    /// document choose its own, so it's read from the first entry rather than assumed.
    /// </summary>
    private (int From, int To, int Indent) Entries((int Start, int End) block)
    {
        for (var line = block.Start + 1; line < block.End; line++)
        {
            if (Indent(_lines[line]) is { } indent)
            {
                return (block.Start + 1, block.End, indent);
            }
        }

        return (block.End, block.End, 0);
    }

    /// <summary>
    /// Whether the line declares the given key, quoted or not.
    /// </summary>
    private static bool Names(string trimmed, string key) =>
        trimmed.StartsWith($"{key}:", StringComparison.Ordinal)
        || trimmed.StartsWith($"\"{key}\":", StringComparison.Ordinal)
        || trimmed.StartsWith($"'{key}':", StringComparison.Ordinal);

    /// <summary>
    /// The lines a key owns: its own, and everything indented under it.
    /// </summary>
    private (int Start, int End) Block((int Line, int Indent) key)
    {
        var end = key.Line + 1;
        var last = end;
        while (end < _lines.Length)
        {
            if (Indent(_lines[end]) is { } indent)
            {
                if (indent <= key.Indent)
                {
                    break;
                }

                // Blank lines and comments inside a block belong to it; ones trailing it don't.
                last = end + 1;
            }

            end++;
        }

        return (key.Line, last);
    }

    /// <summary>
    /// How deeply the line is indented, or null for a line that holds nothing.
    /// </summary>
    private static int? Indent(string line) =>
        string.IsNullOrWhiteSpace(line) ? null : line.Length - line.TrimStart().Length;

    private static IEnumerable<string> Lines(string block) =>
        block.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n').Split('\n');

    private static IEnumerable<string> TrimEnd(IEnumerable<string> lines)
    {
        var list = lines.ToList();
        while (list.Count > 0 && string.IsNullOrWhiteSpace(list[^1]))
        {
            list.RemoveAt(list.Count - 1);
        }

        return list;
    }

    private static YamlMappingNode? Root(string text)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(text));
        return stream.Documents.Count > 0 ? stream.Documents[0].RootNode as YamlMappingNode : null;
    }

    private static YamlNode? Value(YamlMappingNode? mapping, string key) => mapping?.Children
        .FirstOrDefault(child => child.Key is YamlScalarNode { Value: { } name } && name == key)
        .Value;

    private static string? Scalar(YamlMappingNode? mapping, string key) => (Value(mapping, key) as YamlScalarNode)?.Value;

    private static IEnumerable<string> Keys(YamlMappingNode? mapping) => mapping is null
        ? []
        : mapping.Children.Keys.OfType<YamlScalarNode>().Select(key => key.Value).OfType<string>();

    /// <summary>
    /// The workflow-wide <c>defaults.run.working-directory</c>, which is where its jobs run.
    /// </summary>
    private static string? WorkingDirectoryOf(YamlMappingNode? root) =>
        Scalar(Value(Value(root, "defaults") as YamlMappingNode, "run") as YamlMappingNode, "working-directory");

    private static IEnumerable<ActionsJob> JobsOf(YamlMappingNode? jobs)
    {
        if (jobs is null)
        {
            yield break;
        }

        foreach (var (key, value) in jobs.Children)
        {
            if (key is not YamlScalarNode { Value: { } id })
            {
                continue;
            }

            yield return new ActionsJob(id, [.. StepsOf(value as YamlMappingNode)]);
        }
    }

    private static IEnumerable<ActionsStep> StepsOf(YamlMappingNode? job)
    {
        if (Value(job, "steps") is not YamlSequenceNode steps)
        {
            yield break;
        }

        foreach (var step in steps.OfType<YamlMappingNode>())
        {
            if (Scalar(step, "run") is { } run)
            {
                yield return new ActionsStep(run, Scalar(step, "working-directory"));
            }
        }
    }
}
