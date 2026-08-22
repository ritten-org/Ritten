using Ritten.Contracts;
using Ritten.Engine.Workflows;

namespace Ritten.Init;

/// <summary>
/// What a repository's GitHub Actions workflow says about the jobs Ritten runs.
/// </summary>
internal static class ActionsWorkflowTemplate
{
    /// <summary>
    /// The jobs worth putting in CI: what guards a change, and what ships one. The rest are run
    /// by hand, and scaffolding them would be noise.
    /// </summary>
    public static IEnumerable<IJob> Automated(IWorkflow workflow) =>
        workflow.Jobs.Where(job => job.Kind is JobKind.Check or JobKind.Deploy);

    /// <summary>
    /// An empty workflow document, for a repository that hasn't got one to add to.
    /// </summary>
    /// <param name="name">The workflow's name, as the Actions tab will list it.</param>
    public static string Document(string name) => $"name: {name}\n\non:\n\njobs:\n";

    /// <summary>
    /// The events a job of the given kind runs on, as the entries they appear as under <c>on</c>.
    /// </summary>
    /// <param name="job">The job the triggers are for.</param>
    public static IEnumerable<(string Name, string Block)> Triggers(IJob job) => job.Kind switch
    {
        // A release is asked for, never triggered by a change.
        JobKind.Deploy =>
        [
            ("workflow_dispatch",
                """
                  workflow_dispatch:
                    inputs:
                      dry-run:
                        description: 'Dry Run'
                        type: boolean
                        default: false
                """)
        ],

        // A check guards the change that triggered it.
        JobKind.Check =>
        [
            ("pull_request",
                """
                  pull_request:
                    branches: [ main ]
                """),
            ("push",
                """
                  push:
                    branches: [ main ]
                """)
        ],
        _ => []
    };

    /// <summary>
    /// The job, as it appears under <c>jobs</c>.
    /// </summary>
    /// <param name="job">The job to render.</param>
    /// <param name="tool">The tool the job runs.</param>
    /// <param name="directory">The project's directory, when it isn't the repository's root.</param>
    /// <param name="globalJson">The SDK version file, when the repository has one.</param>
    public static string Job(IJob job, ToolPin tool, string? directory, string? globalJson) => job.Kind switch
    {
        JobKind.Deploy => Deploy(job, tool, directory, globalJson),
        _ => Check(job, tool, directory, globalJson)
    };

    /// <summary>
    /// A job that guards every change: it runs on the change, and a newer push supersedes it.
    /// </summary>
    private static string Check(IJob job, ToolPin tool, string? directory, string? globalJson) => Lines([
        $"  {job.Name}:",
        $"    name: {Title(job.Name)}",
        "    runs-on: ubuntu-latest",
        "    if: github.event_name == 'pull_request' || github.event_name == 'push'",
        "    permissions:",
        "      contents: read",
        "      # The report is posted as a pull request comment.",
        "      pull-requests: write",
        "    concurrency:",
        "      # A newer push to the same branch supersedes any run still going. The workflow's own",
        "      # name keeps one project's runs from cancelling another's.",
        $"      group: ${{{{ github.workflow }}}}-{job.Name}-${{{{ github.ref }}}}",
        "      cancel-in-progress: true",
        "    steps:",
        .. Setup(directory, globalJson),
        "",
        $"      - name: Run {job.Name}",
        $"        run: dotnet {tool.Command} {job.Name}",
        "        shell: bash",
        .. In(directory),
        "        env:",
        "          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}"
    ]);

    /// <summary>
    /// A job that releases: asked for deliberately, queued rather than cancelled, and approved up
    /// front because there's nobody at the terminal to confirm.
    /// </summary>
    private static string Deploy(IJob job, ToolPin tool, string? directory, string? globalJson) => Lines([
        $"  {job.Name}:",
        $"    name: {Title(job.Name)}",
        "    runs-on: ubuntu-latest",
        "    if: github.event_name == 'workflow_dispatch' && github.ref == 'refs/heads/main'",
        "    permissions:",
        "      # Tags and releases are written back to the repository.",
        "      contents: write",
        "      id-token: write",
        "    concurrency:",
        "      # Releases queue rather than interleave.",
        $"      group: ${{{{ github.workflow }}}}-{job.Name}",
        "      cancel-in-progress: false",
        "    steps:",
        .. Setup(directory, globalJson),
        "",
        "      - name: NuGet login",
        "        uses: NuGet/login@v1",
        "        id: nuget-login",
        "        with:",
        "          user: ${{ secrets.NUGET_USER }}",
        "",
        $"      - name: Run {job.Name}",
        $"        run: dotnet {tool.Command} {job.Name} --auto-approve ${{{{ inputs['dry-run'] && '--dry-run' || '' }}}}",
        "        shell: bash",
        .. In(directory),
        "        env:",
        "          RITTEN_NUGET_API_KEY: ${{ steps.nuget-login.outputs.NUGET_API_KEY }}",
        "          RITTEN_COMMIT_SHA: ${{ github.sha }}",
        "          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}"
    ]);

    /// <summary>
    /// Checking out, and getting the tool that runs the job. The SDK version is whatever
    /// <c>global.json</c> pins, so the workflow never names one — and says nothing at all when
    /// the repository doesn't pin one, rather than pointing at a file that isn't there.
    /// </summary>
    private static IEnumerable<string> Setup(string? directory, string? globalJson) =>
    [
        "      - name: Checkout repository",
        "        uses: actions/checkout@v7",
        "",
        "      - name: Set up .NET",
        "        uses: actions/setup-dotnet@v6",
        .. globalJson is null ? Array.Empty<string>() : ["        with:", $"          global-json-file: {globalJson}"],
        "",
        "      - name: Restore tools",
        "        run: dotnet tool restore",
        .. In(directory)
    ];

    /// <summary>
    /// Where a step runs, for a project that isn't the repository. Everything Ritten needs is
    /// found by walking up from here: the project file, the tool manifest, the repository itself.
    /// </summary>
    private static IEnumerable<string> In(string? directory) =>
        directory is null ? [] : [$"        working-directory: {directory}"];

    private static string Lines(IEnumerable<string> lines) => string.Join('\n', lines);

    private static string Title(string name) => string.Concat(char.ToUpperInvariant(name[0]), name[1..]);
}
