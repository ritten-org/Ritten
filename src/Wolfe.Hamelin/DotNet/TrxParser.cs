using System.Xml.Linq;

namespace Wolfe.Hamelin.DotNet;

/// <summary>
/// Parses TRX test result files. Elements are matched by local name, so the TRX namespace
/// version doesn't matter.
/// </summary>
internal static class TrxParser
{
    public static async Task<TestRun> Parse(Stream stream, CancellationToken cancellationToken)
    {
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        var counters = document.Descendants().FirstOrDefault(e => e.Name.LocalName == "Counters");
        int Count(string attribute) => (int?)counters?.Attribute(attribute) ?? 0;

        var failures = document.Descendants()
            .Where(e => e.Name.LocalName == "UnitTestResult" && (string?)e.Attribute("outcome") == "Failed")
            .Select(e => new TestFailure(
                (string?)e.Attribute("testName") ?? "(unknown test)",
                e.Descendants().FirstOrDefault(m => m.Name.LocalName == "Message")?.Value.Trim() ?? ""))
            .ToList();

        return new TestRun
        {
            Passed = Count("passed"),
            Failed = Count("failed"),
            Skipped = Count("notExecuted"),
            Failures = failures
        };
    }
}
