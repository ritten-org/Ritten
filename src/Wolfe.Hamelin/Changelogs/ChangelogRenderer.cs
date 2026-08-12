namespace Wolfe.Hamelin.Changelogs;

/// <summary>
/// Renders changelogs back to markdown.
/// </summary>
/// <remarks>
/// Entries parsed from a file render their raw <see cref="ChangelogEntry.Body"/> verbatim;
/// entries built in code without a body are synthesized from their structured sections.
/// </remarks>
internal static class ChangelogRenderer
{
    public static string Render(Changelog changelog)
    {
        var blocks = new List<string>();
        if (!string.IsNullOrWhiteSpace(changelog.Preamble))
        {
            blocks.Add(changelog.Preamble.Trim('\n'));
        }

        foreach (var entry in changelog.Entries)
        {
            blocks.Add(RenderHeading(entry));
            var body = RenderEntry(entry);
            if (body.Length > 0)
            {
                blocks.Add(body);
            }
        }

        if (!string.IsNullOrWhiteSpace(changelog.LinkReferences))
        {
            blocks.Add(changelog.LinkReferences.Trim('\n'));
        }

        return string.Join("\n\n", blocks) + "\n";
    }

    public static string RenderEntry(ChangelogEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Body))
        {
            return entry.Body.Trim('\n');
        }

        var blocks = new List<string>();
        if (!string.IsNullOrWhiteSpace(entry.Preamble))
        {
            blocks.Add(entry.Preamble.Trim('\n'));
        }

        AddSection(blocks, "Added", entry.Added);
        AddSection(blocks, "Changed", entry.Changed);
        AddSection(blocks, "Deprecated", entry.Deprecated);
        AddSection(blocks, "Removed", entry.Removed);
        AddSection(blocks, "Fixed", entry.Fixed);
        AddSection(blocks, "Security", entry.Security);

        return string.Join("\n\n", blocks);
    }

    private static string RenderHeading(ChangelogEntry entry)
    {
        var label = entry.Version?.ToString() ?? "Unreleased";
        return entry.Date is { } date ? $"## [{label}] - {date:yyyy-MM-dd}" : $"## [{label}]";
    }

    private static void AddSection(List<string> blocks, string name, IReadOnlyCollection<string> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        blocks.Add($"### {name}\n\n{string.Join('\n', items.Select(i => $"- {i}"))}");
    }
}
