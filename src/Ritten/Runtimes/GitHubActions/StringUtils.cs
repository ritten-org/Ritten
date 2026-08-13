namespace Ritten.Runtimes.GitHubActions;

internal static class StringUtils
{
    public const string UrlEncodedNewLine = "%0A";

    public static string SanitizeNewLines(string input) => input.Replace("\r", "").Replace("\n", UrlEncodedNewLine);
}
