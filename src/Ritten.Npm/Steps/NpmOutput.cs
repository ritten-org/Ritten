namespace Ritten.Npm.Steps;

/// <summary>
/// A script's output as a report shows it.
/// </summary>
internal static class NpmOutput
{
    /// <summary>
    /// How many of its last lines a failure shows: where the errors are, without the whole log.
    /// </summary>
    internal const int Lines = 100;

    /// <summary>
    /// The end of <paramref name="output"/>, fenced as code.
    /// </summary>
    public static string Fenced(string output)
    {
        var lines = output.TrimEnd().Split('\n');
        var shown = lines.Length > Lines ? ["…", .. lines[^Lines..]] : lines;
        return $"```\n{string.Join('\n', shown)}\n```";
    }
}
