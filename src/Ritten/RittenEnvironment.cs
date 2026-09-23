namespace Ritten;

/// <summary>
/// The environment variables Ritten defines.
/// </summary>
internal static class RittenEnvironment
{
    /// <summary>
    /// The API key used to push packages.
    /// </summary>
    public const string NuGetApiKey = NuGet.NuGetOptions.ApiKeyVariable;
}
