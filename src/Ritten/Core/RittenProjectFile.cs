namespace Ritten.Core;

internal sealed record RittenProjectFile
{
    public string? Project { get; init; }
    public string Configuration { get; init; } = "Release";
    public string Changelog { get; init; } = "CHANGELOG.md";
    public string? Repository { get; init; }
    public string TagPrefix { get; init; } = "v";
    public string Feed { get; init; } = "https://api.nuget.org/v3/index.json";
}
