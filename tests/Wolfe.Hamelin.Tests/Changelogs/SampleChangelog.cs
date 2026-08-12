namespace Wolfe.Hamelin.Tests.Changelogs;

/// <summary>
/// A representative Keep a Changelog document used across the changelog tests, loaded from
/// <c>SampleChangelog.md</c>. Canonically formatted (single blank lines, LF endings, trailing
/// newline) so it round-trips byte-for-byte.
/// </summary>
public static class SampleChangelog
{
    public static readonly string Text = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Changelogs", "SampleChangelog.md"));
}
