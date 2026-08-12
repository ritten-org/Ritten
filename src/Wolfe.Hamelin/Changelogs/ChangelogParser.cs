using System.Globalization;
using System.Text.RegularExpressions;
using NuGet.Versioning;

namespace Wolfe.Hamelin.Changelogs;

/// <summary>
/// Parses https://keepachangelog.com/en/1.1.0/ formatted changelogs.
/// </summary>
/// <remarks>
/// Parsing is lenient: only the version headings themselves must be well-formed.
/// Section headings the format doesn't define, nested lists, and any other markdown
/// are preserved on <see cref="ChangelogEntry.Body"/> even when they don't fit the structured view.
/// </remarks>
internal static partial class ChangelogParser
{
    public static Changelog Parse(string changelog)
    {
        var lines = SplitLines(changelog);
        var links = ExtractTrailingLinks(lines, out var contentLength);

        var headings = new List<int>();
        for (var i = 0; i < contentLength; i++)
        {
            if (EntryHeading().IsMatch(lines[i]))
            {
                headings.Add(i);
            }
        }

        var entries = new List<ChangelogEntry>();
        for (var h = 0; h < headings.Count; h++)
        {
            var end = h + 1 < headings.Count ? headings[h + 1] : contentLength;
            entries.Add(ParseEntryAt(lines, headings[h], end));
        }

        return new Changelog
        {
            Preamble = Join(lines, 0, headings.Count > 0 ? headings[0] : contentLength),
            Entries = entries,
            Links = links
        };
    }

    public static ChangelogEntry ParseEntry(string entry)
    {
        var lines = SplitLines(entry);
        var start = 0;
        while (start < lines.Length && string.IsNullOrWhiteSpace(lines[start]))
        {
            start++;
        }

        return start < lines.Length && EntryHeading().IsMatch(lines[start])
            ? ParseEntryAt(lines, start, lines.Length)
            : ParseBody(null, null, lines, start, lines.Length);
    }

    private static ChangelogEntry ParseEntryAt(string[] lines, int headingIndex, int end)
    {
        var heading = EntryHeading().Match(lines[headingIndex]);
        var label = heading.Groups["label"].Value.Trim();

        NuGetVersion? version = null;
        if (!label.Equals("Unreleased", StringComparison.OrdinalIgnoreCase))
        {
            if (!NuGetVersion.TryParse(label, out var parsed))
            {
                throw new FormatException($"Invalid version '{label}' in changelog heading '{lines[headingIndex].Trim()}'.");
            }

            version = parsed;
        }

        DateOnly? date = null;
        var dateText = heading.Groups["date"].Value.Trim();
        if (dateText.Length > 0 && DateOnly.TryParse(dateText, CultureInfo.InvariantCulture, out var parsedDate))
        {
            date = parsedDate;
        }

        return ParseBody(version, date, lines, headingIndex + 1, end);
    }

    private static ChangelogEntry ParseBody(NuGetVersion? version, DateOnly? date, string[] lines, int start, int end)
    {
        var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var preamble = new List<string>();
        List<string>? currentItems = null;

        for (var i = start; i < end; i++)
        {
            var line = lines[i];

            var section = SectionHeading().Match(line);
            if (section.Success)
            {
                var name = section.Groups["name"].Value.Trim();
                if (!sections.TryGetValue(name, out currentItems))
                {
                    currentItems = [];
                    sections[name] = currentItems;
                }

                continue;
            }

            if (currentItems is null)
            {
                preamble.Add(line);
                continue;
            }

            var item = ItemLine().Match(line);
            if (item.Success)
            {
                currentItems.Add(item.Groups["text"].Value.TrimEnd());
            }
            else if (!string.IsNullOrWhiteSpace(line) && currentItems.Count > 0)
            {
                // A continuation of the previous item: an indented line, nested bullet, etc.
                currentItems[^1] += "\n" + line.TrimEnd();
            }
        }

        return new ChangelogEntry
        {
            Version = version,
            Date = date,
            Body = Join(lines, start, end),
            Preamble = string.Join('\n', preamble).Trim('\n'),
            Added = Section("Added"),
            Changed = Section("Changed"),
            Deprecated = Section("Deprecated"),
            Removed = Section("Removed"),
            Fixed = Section("Fixed"),
            Security = Section("Security")
        };

        IReadOnlyCollection<string> Section(string name) =>
            sections.TryGetValue(name, out var items) ? items : [];
    }

    /// <summary>
    /// Splits off the block of reference-style link definitions at the end of the file, so they
    /// don't end up in the last entry's body.
    /// </summary>
    private static List<ChangelogLink> ExtractTrailingLinks(string[] lines, out int contentLength)
    {
        var first = lines.Length;
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            if (!LinkReferenceLine().IsMatch(lines[i]))
            {
                break;
            }

            first = i;
        }

        contentLength = first;
        return lines[first..]
            .Select(l => LinkReferenceLine().Match(l))
            .Where(m => m.Success)
            .Select(m => new ChangelogLink(m.Groups["label"].Value, m.Groups["url"].Value))
            .ToList();
    }

    private static string[] SplitLines(string text) => text.Replace("\r\n", "\n").Split('\n');

    private static string Join(string[] lines, int start, int end) =>
        string.Join('\n', lines[start..end]).Trim('\n');

    [GeneratedRegex(@"^##\s*\[(?<label>[^\]]+)\](?:\s*-\s*(?<date>.+?))?\s*$")]
    private static partial Regex EntryHeading();

    [GeneratedRegex(@"^###\s+(?<name>.+?)\s*$")]
    private static partial Regex SectionHeading();

    [GeneratedRegex(@"^[-*+]\s+(?<text>.*)$")]
    private static partial Regex ItemLine();

    [GeneratedRegex(@"^\[(?<label>[^\]]+)\]:\s*(?<url>\S+)\s*$")]
    private static partial Regex LinkReferenceLine();
}
