using System.Reflection;
using NuGet.Versioning;

namespace Ritten.Init;

/// <summary>
/// This tool, as the repositories it sets up will pin it.
/// </summary>
internal static class RittenTool
{
    /// <summary>
    /// The name the tool is published and invoked under.
    /// </summary>
    private const string Name = "ritten";

    /// <summary>
    /// The version doing the setting up is the one the repository gets pinned to: what a
    /// repository runs should be what wrote down how to run it.
    /// </summary>
    public static ToolPin Pin { get; } = new(Name, Name, Version());

    private static NuGetVersion Version()
    {
        var assembly = typeof(RittenTool).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0];
        return NuGetVersion.TryParse(informational ?? assembly.GetName().Version?.ToString(3), out var version)
            ? version
            : new NuGetVersion(0, 0, 0);
    }
}
