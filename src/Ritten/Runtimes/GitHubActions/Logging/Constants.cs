using Microsoft.Extensions.Logging;

namespace Ritten.Runtimes.GitHubActions.Logging;

internal static class Constants
{
    public const string FormatterName = "GitHubActions";
    public static readonly EventId RawCommandEventId = new(0, "GitHubActionsRawCommand");
}
