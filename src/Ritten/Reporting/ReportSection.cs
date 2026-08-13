namespace Ritten.Reporting;

/// <summary>
/// A titled section of the build report.
/// </summary>
public class ReportSection(string title)
{
    private readonly List<ReportEntry> _entries = [];

    /// <summary>
    /// The title of the section.
    /// </summary>
    public string Title { get; } = title;

    /// <summary>
    /// The entries added to the section, in the order they were added.
    /// </summary>
    public IReadOnlyList<ReportEntry> Entries => _entries;

    /// <summary>
    /// The overall tone of the section: the most severe tone of any entry within it.
    /// </summary>
    public ReportTone Tone => _entries.Count == 0 ? ReportTone.Note : _entries.Max(e => e.Tone);

    /// <summary>
    /// Adds a note to the section.
    /// </summary>
    public ReportSection Note(string markdown) => Add(new ReportParagraph(ReportTone.Note, markdown));

    /// <summary>
    /// Adds a success note to the section.
    /// </summary>
    public ReportSection Success(string markdown) => Add(new ReportParagraph(ReportTone.Success, markdown));

    /// <summary>
    /// Adds a warning note to the section.
    /// </summary>
    public ReportSection Warning(string markdown) => Add(new ReportParagraph(ReportTone.Warning, markdown));

    /// <summary>
    /// Adds a failure note to the section.
    /// </summary>
    public ReportSection Failure(string markdown) => Add(new ReportParagraph(ReportTone.Failure, markdown));

    /// <summary>
    /// Adds a detail block to the section.
    /// </summary>
    /// <param name="summary">A title for the detail block.</param>
    /// <param name="markdown">The content/detail of the block.</param>
    /// <param name="tone">The tone of the block.</param>
    public ReportSection Details(string summary, string markdown, ReportTone tone = ReportTone.Note) =>
        Add(new ReportDetailsBlock(tone, summary, markdown));

    private ReportSection Add(ReportEntry entry)
    {
        _entries.Add(entry);
        return this;
    }
}
