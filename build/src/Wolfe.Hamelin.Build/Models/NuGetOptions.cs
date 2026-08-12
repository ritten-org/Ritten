namespace Wolfe.Hamelin.Build.Models;

// Bound from the "NuGet" section.
public class NuGetOptions
{
    public string Feed { get; set; } = "https://api.nuget.org/v3/index.json";

    /// <summary>
    /// Only needed by the deploy pipeline; Publish fails with a clear message if it's missing.
    /// </summary>
    public string? ApiKey { get; set; }

    public bool SkipVersionCheck { get; set; }
}
