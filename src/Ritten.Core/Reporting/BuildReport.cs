namespace Ritten.Reporting;

internal class BuildReport : IBuildReport
{
    private readonly List<ReportSection> _sections = [];

    public IReadOnlyList<ReportSection> Sections => _sections;

    public ReportSection Section(string title)
    {
        var existing = _sections.FirstOrDefault(s => s.Title == title);
        if (existing != null)
        {
            return existing;
        }

        var section = new ReportSection(title);
        _sections.Add(section);
        return section;
    }
}
