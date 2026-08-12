namespace Wolfe.Hamelin.Reporting;

internal static class ReportToneExtensions
{
    public static string Icon(this ReportTone tone) => tone switch
    {
        ReportTone.Note => "ℹ️",
        ReportTone.Success => "✅",
        ReportTone.Warning => "⚠️",
        ReportTone.Failure => "❌",
        _ => throw new ArgumentOutOfRangeException(nameof(tone), tone, null)
    };
}
