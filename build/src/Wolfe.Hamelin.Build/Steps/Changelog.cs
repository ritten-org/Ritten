using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Build.Reporting;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Validate Changelog Entry")]
public class Changelog(
    ILogger<Changelog> logger,
    IOptions<BuildOptions> options,
    IPipelineContext context,
    IBuildReport report
) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        if (options.Value.SkipChangelog)
        {
            logger.LogInformation("Skipping changelog check.");
            return;
        }

        var projectInfo = context.State.Get<ProjectInfo>();
        if (projectInfo == null)
        {
            throw new Exception("Project info not found in state.");
        }

        var changelog = context.FileSystem.CurrentDirectory.GetFile(options.Value.ChangelogFile);
        if (!changelog.Exists)
        {
            report.Section("Release").Failure(
                $"The changelog file `{options.Value.ChangelogFile}` doesn't exist — create it with a `## [{projectInfo.Version}]` entry describing the release.");
            throw new FileNotFoundException("Could not find changelog file", changelog.AbsolutePath);
        }

        var lines = await File.ReadAllLinesAsync(changelog.AbsolutePath, cancellationToken);

        string[] candidateHeadings = projectInfo.Version.IsPrerelease
            ? [$"## [{projectInfo.Version}]", "## [Unreleased]"]
            : [$"## [{projectInfo.Version}]"];

        var headingIndex = -1;
        foreach (var candidate in candidateHeadings)
        {
            headingIndex = Array.FindIndex(lines, l => l.StartsWith(candidate, StringComparison.Ordinal));
            if (headingIndex >= 0)
            {
                break;
            }
        }

        if (headingIndex < 0)
        {
            report.Section("Release").Failure(
                $"There's no changelog entry for **{projectInfo.Version}** in `{options.Value.ChangelogFile}` — I looked for a heading starting with " +
                $"{string.Join(" or ", candidateHeadings.Select(h => $"`{h}`"))}. Add one describing the release and push again.");
            throw new Exception(
                $"No changelog entry found for version {projectInfo.Version} in {options.Value.ChangelogFile}. Expected a heading starting with one of: {string.Join(", ", candidateHeadings.Select(h => $"'{h}'"))}.");
        }

        var nextHeadingIndex = Array.FindIndex(lines, headingIndex + 1, l => l.StartsWith("## [", StringComparison.Ordinal));
        var endIndex = nextHeadingIndex < 0 ? lines.Length : nextHeadingIndex;
        var body = string.Join('\n', lines[(headingIndex + 1)..endIndex]).Trim();

        if (string.IsNullOrWhiteSpace(body))
        {
            report.Section("Release").Failure(
                $"The changelog entry for **{projectInfo.Version}** is empty — add at least one line describing the release.");
            throw new Exception($"Changelog entry for version {projectInfo.Version} is empty.");
        }

        context.State.Set(new ChangelogEntry { Version = projectInfo.Version, Body = body });
        report.Section("Release").Success($"Changelog entry for **{projectInfo.Version}** is present.");
        logger.LogInformation("Found changelog entry for {Version} ({Length} chars).", projectInfo.Version, body.Length);
    }
}
