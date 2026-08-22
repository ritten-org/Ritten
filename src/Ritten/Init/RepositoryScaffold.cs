using System.Text.Json;
using System.Text.Json.Nodes;
using Ritten.Engine.Workflows;

namespace Ritten.Init;

/// <summary>
/// What a repository needs in order to run a Ritten workflow.
/// </summary>
internal static class RepositoryScaffold
{
    /// <summary>
    /// Where the tool manifest lives, by dotnet's convention.
    /// </summary>
    public const string ToolManifest = ".config/dotnet-tools.json";

    /// <summary>
    /// Where the GitHub Actions workflow lives, by GitHub's convention.
    /// </summary>
    public const string ActionsWorkflow = ".github/workflows/ritten.yml";

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    /// <summary>
    /// Every file the repository should have, and what it should say.
    /// </summary>
    /// <param name="workflow">The workflow the repository runs.</param>
    /// <param name="project">The project file the repository ships, when one was found.</param>
    /// <param name="version">The version of Ritten to pin, which is the one doing the scaffolding.</param>
    /// <param name="projectFile">The name the host gives the project file.</param>
    public static IReadOnlyList<ScaffoldedFile> For(IWorkflow workflow, string? project, string version, string projectFile) =>
    [
        new(projectFile, RittenJson(workflow, project)),
        new("CHANGELOG.md", Changelog()),
        new(ToolManifest, Manifest(version), Generated: true),
        new(ActionsWorkflow, WorkflowYaml.Render(workflow), Generated: true)
    ];

    private static string RittenJson(IWorkflow workflow, string? project)
    {
        var json = new JsonObject { ["workflow"] = workflow.Name };

        // A workflow with nothing to release needs no project: it builds whatever it finds.
        if (project is not null)
        {
            json["build"] = new JsonObject { ["project"] = project };
        }

        return json.ToJsonString(Indented) + "\n";
    }

    private static string Changelog() =>
        """
        # Changelog

        All notable changes to this project will be documented in this file.

        The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

        ## [Unreleased]

        """;

    private static string Manifest(string version)
    {
        var manifest = new JsonObject
        {
            ["version"] = 1,
            ["isRoot"] = true,
            ["tools"] = new JsonObject
            {
                ["ritten"] = new JsonObject
                {
                    ["version"] = version,
                    ["commands"] = new JsonArray("ritten"),
                    ["rollForward"] = false
                }
            }
        };

        return manifest.ToJsonString(Indented) + "\n";
    }
}
