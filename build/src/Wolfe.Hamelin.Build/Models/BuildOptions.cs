namespace Wolfe.Hamelin.Build.Models;

public class BuildOptions
{
    public required string ArtifactsDirectory { get; set; }
    public required string TempDirectory { get; set; }
    public required string Configuration { get; set; }
    public required string ProjectFile { get; set; }
    public required string ChangelogFile { get; set; }
    public required string NuGetFeed { get; set; }
    public required string NuGetApiKey { get; set; }

    // The repository's web URL; when set, the changelog's version links are validated against it.
    public string? RepositoryUrl { get; set; }
    public string? CommitSha { get; set; }
    public bool SkipChangelog { get; set; } = false;
    public bool SkipVersion { get; set; } = false;
}
