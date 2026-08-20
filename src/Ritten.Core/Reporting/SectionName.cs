namespace Ritten.Reporting;

/// <summary>
/// The title of a report section. Sections accumulate by title, so two steps writing the same
/// section have to spell it identically — a shared vocabulary keeps a rename from silently
/// splitting one section in two. The list is open: a host names its own with <see cref="Named"/>.
/// </summary>
public sealed class SectionName
{
    private SectionName(string title) => Title = title;

    /// <summary>
    /// Restoring the project's dependencies.
    /// </summary>
    public static SectionName Restore { get; } = Named("Restore");

    /// <summary>
    /// Compiling the project.
    /// </summary>
    public static SectionName Build { get; } = Named("Build");

    /// <summary>
    /// Formatting and style.
    /// </summary>
    public static SectionName Formatting { get; } = Named("Formatting");

    /// <summary>
    /// The outcome of the test run.
    /// </summary>
    public static SectionName Tests { get; } = Named("Tests");

    /// <summary>
    /// Code coverage and its thresholds.
    /// </summary>
    public static SectionName Coverage { get; } = Named("Coverage");

    /// <summary>
    /// The version being released, and whether it's releasable.
    /// </summary>
    public static SectionName Version { get; } = Named("Version");

    /// <summary>
    /// The metadata a package carries to the feed.
    /// </summary>
    public static SectionName Metadata { get; } = Named("Metadata");

    /// <summary>
    /// The changelog and its entries.
    /// </summary>
    public static SectionName Changelog { get; } = Named("Changelog");

    /// <summary>
    /// What a deploy published.
    /// </summary>
    public static SectionName Release { get; } = Named("Release");

    /// <summary>
    /// The section's title, as it appears in the report.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Names a section this vocabulary doesn't carry, for a host reporting on its own domain.
    /// </summary>
    /// <param name="title">The title the section appears under.</param>
    public static SectionName Named(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        return new SectionName(title);
    }

    /// <summary>
    /// Reads the name as its title, so it can title a section wherever one is asked for.
    /// </summary>
    /// <param name="name">The name to read.</param>
    public static implicit operator string(SectionName name) => name.Title;

    /// <inheritdoc />
    public override string ToString() => Title;
}
