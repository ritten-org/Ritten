using System.Text.RegularExpressions;

namespace Ritten.DotNet;

/// <summary>
/// Extracts structured facts from <c>dotnet</c> command output.
/// </summary>
internal static partial class DotNetOutputParser
{
    public static IReadOnlyList<DotNetDiagnostic> ParseDiagnostics(string output)
    {
        return output
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(line => DiagnosticLine().Match(line.Trim()))
            .Where(m => m.Success)
            .Select(m => new DotNetDiagnostic
            {
                Severity = m.Groups["severity"].Value == "error" ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
                Code = m.Groups["code"].Value,
                Message = m.Groups["message"].Value,
                File = m.Groups["file"].Success ? m.Groups["file"].Value : null,
                Line = m.Groups["line"].Success ? int.Parse(m.Groups["line"].Value) : null,
                Column = m.Groups["column"].Success ? int.Parse(m.Groups["column"].Value) : null
            })
            // Multi-targeted projects repeat every diagnostic per framework; the trailing
            // `[project]` suffix isn't captured, so the duplicates collapse here.
            .Distinct()
            .ToList();
    }

    // Matches `path(line,col): error CS0103: message [project.csproj]` as well as locationless
    // forms like `error NU1101: message` and `MSBUILD : error MSB1009: message`.
    [GeneratedRegex(@"^(?:(?<file>.+?)\((?<line>\d+),(?<column>\d+)\))?.*?\b(?<severity>error|warning)\b\s+(?<code>[A-Za-z]+\d+):\s*(?<message>.+?)(?:\s+\[[^\]]+\])?$")]
    private static partial Regex DiagnosticLine();

    /// <summary>
    /// Extracts the projects a <c>dotnet restore</c> actually restored. An up-to-date restore
    /// mentions none.
    /// </summary>
    public static IReadOnlyList<string> ParseRestoredProjects(string output)
    {
        return output
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(line => RestoredLine().Match(line.Trim()))
            .Where(m => m.Success)
            .Select(m => Path.GetFileNameWithoutExtension(m.Groups["path"].Value))
            .ToList();
    }

    // Matches `Restored /path/to/Thing.csproj (in 407 ms).`
    [GeneratedRegex(@"^Restored (?<path>.+?proj) \(in .+\)\.?$")]
    private static partial Regex RestoredLine();
}
