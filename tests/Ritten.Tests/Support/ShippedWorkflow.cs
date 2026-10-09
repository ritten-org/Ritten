using System.Text.Json;
using Ritten.Engine;
using Ritten.Engine.Workflows;
using Ritten.Workflows;

namespace Ritten.Tests.Support;

/// <summary>
/// Reads a shipped workflow's job arguments the way its command line would, without running anything.
/// </summary>
internal static class ShippedWorkflow
{
    /// <summary>
    /// Builds the tool's application, then reads and judges the arguments a job would run with.
    /// </summary>
    /// <param name="workflow">The workflow's name.</param>
    /// <param name="job">The job's name.</param>
    /// <param name="settings">The project file's contents.</param>
    /// <param name="environment">The process environment; every variable set unless given.</param>
    /// <param name="dryRun">Whether the run would be a rehearsal.</param>
    public static Result<object> ReadArguments(string workflow, string job, string settings, Func<string, string?>? environment = null, bool dryRun = false)
    {
        // Building also checks every step of every workflow against the services they need.
        var builder = WorkflowApplication.CreateBuilder();
        builder.AddDotNetWorkflows();
        using var application = builder.Build(environment ?? (_ => "set")).Value.ShouldNotBeNull();

        var declared = application.Workflows.Single(w => w.Name == workflow);
        var project = new RittenProject { Directory = Path.GetTempPath(), Settings = JsonSerializer.Deserialize<JsonElement>(settings) };
        return application.ReadArguments(
            new SelectedWorkflow(declared, project),
            declared.Jobs.Single(j => j.Name == job),
            new RunJobArgs(job) { DryRun = dryRun },
            new RecordingConsole());
    }
}
