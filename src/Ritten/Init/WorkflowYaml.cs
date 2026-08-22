using System.Text;
using Ritten.Contracts;
using Ritten.Engine.Workflows;

namespace Ritten.Init;

/// <summary>
/// Renders the GitHub Actions workflow that runs a Ritten workflow's jobs.
/// </summary>
internal static class WorkflowYaml
{
    /// <summary>
    /// The .NET SDK is pinned by global.json, so the workflow never names a version.
    /// </summary>
    private const string Setup =
        """
              - name: Checkout repository
                uses: actions/checkout@v7

              - name: Set up .NET
                uses: actions/setup-dotnet@v6
                with:
                  global-json-file: global.json

              - name: Restore tools
                run: dotnet tool restore
        """;

    /// <summary>A release is asked for, never triggered by a change.</summary>
    private const string Dispatch =
        """
          workflow_dispatch:
            inputs:
              dry-run:
                description: 'Dry Run'
                type: boolean
                default: false
        """;

    /// <summary>A check guards the change that triggered it.</summary>
    private const string OnChange =
        """
          pull_request:
            branches: [ main ]
          push:
            branches: [ main ]
        """;

    public static string Render(IWorkflow workflow)
    {
        var checks = workflow.Jobs.Where(job => job.Kind == JobKind.Check).ToList();
        var deploys = workflow.Jobs.Where(job => job.Kind == JobKind.Deploy).ToList();

        var yaml = new StringBuilder();
        yaml.Append("name: Ritten").Append('\n').Append('\n').Append("on:").Append('\n');

        if (deploys.Count > 0)
        {
            yaml.Append(Dispatch).Append('\n');
        }

        if (checks.Count > 0)
        {
            yaml.Append(OnChange).Append('\n');
        }

        yaml.Append('\n').Append("jobs:").Append('\n');
        foreach (var job in checks)
        {
            yaml.Append(Check(job));
        }

        foreach (var job in deploys)
        {
            yaml.Append(Deploy(job));
        }

        // Each job renders with a blank line after it, which leaves one too many at the end.
        return yaml.ToString().TrimEnd() + "\n";
    }

    /// <summary>
    /// A job that guards every change: it runs on the change, and a newer push supersedes it.
    /// </summary>
    private static string Check(IJob job) =>
        $$$"""
          {{{job.Name}}}:
            name: {{{Title(job.Name)}}}
            runs-on: ubuntu-latest
            if: github.event_name == 'pull_request' || github.event_name == 'push'
            permissions:
              contents: read
              # The report is posted as a pull request comment.
              pull-requests: write
            concurrency:
              # A newer push to the same branch supersedes any run still going.
              group: {{{job.Name}}}-${{ github.ref }}
              cancel-in-progress: true
            steps:
        {{{Setup}}}

              - name: Run {{{job.Name}}}
                run: dotnet ritten {{{job.Name}}}
                shell: bash
                env:
                  GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}


        """;

    /// <summary>
    /// A job that releases: asked for deliberately, queued rather than cancelled, and approved up
    /// front because there's nobody at the terminal to confirm.
    /// </summary>
    private static string Deploy(IJob job) =>
        $$$"""
          {{{job.Name}}}:
            name: {{{Title(job.Name)}}}
            runs-on: ubuntu-latest
            if: github.event_name == 'workflow_dispatch' && github.ref == 'refs/heads/main'
            permissions:
              # Tags and releases are written back to the repository.
              contents: write
              id-token: write
            concurrency:
              # Releases queue rather than interleave.
              group: {{{job.Name}}}
              cancel-in-progress: false
            steps:
        {{{Setup}}}

              - name: NuGet login
                uses: NuGet/login@v1
                id: nuget-login
                with:
                  user: ${{ secrets.NUGET_USER }}

              - name: Run {{{job.Name}}}
                run: dotnet ritten {{{job.Name}}} --auto-approve ${{ inputs['dry-run'] && '--dry-run' || '' }}
                shell: bash
                env:
                  RITTEN_NUGET_API_KEY: ${{ steps.nuget-login.outputs.NUGET_API_KEY }}
                  RITTEN_COMMIT_SHA: ${{ github.sha }}
                  GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}


        """;

    private static string Title(string name) => string.Concat(char.ToUpperInvariant(name[0]), name[1..]);
}
