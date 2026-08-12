namespace Wolfe.Hamelin.Changelogs;

internal class ChangelogParser(string changelog)
{
    private Changelog Parse()
    {
        // TODO: parse changelog
        if (changelog.Length == 0)
        {
            return null!;
        }
        return null!;
    }

    private ChangelogEntry ParseEntry()
    {
        // TODO: Implement.
        return null!;
    }

    public static Changelog Parse(string changelog)
    {
        var parser = new ChangelogParser(changelog);
        return parser.Parse();
    }

    public static ChangelogEntry ParseEntry(string entry)
    {
        var parser = new ChangelogParser(entry);
        return parser.ParseEntry();
    }
}
