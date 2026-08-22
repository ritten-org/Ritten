using NuGet.Versioning;
using Ritten.Engine.Workflows;
using Ritten.GitHub;
using Ritten.Init;
using Ritten.Workflows.DotNet;
using Ritten.Workflows.DotNetTool;

namespace Ritten.Tests.Init;

/// <summary>
/// The whole of what a repository is handed, as the file itself rather than as assertions about
/// it. These snapshots are here to be read: the derivation is only worth trusting if you can see
/// what it produces.
/// </summary>
public class ActionsWorkflowTemplateTests
{
    private static readonly ToolPin Tool = new("ritten", "ritten", NuGetVersion.Parse("1.2.3"));

    [Fact]
    public Task WritesTheWorkflowForARepositoryThatShipsATool() => VerifyWorkflow(new DotNetToolWorkflow(), "My.Tool", directory: null);

    [Fact]
    public Task WritesTheWorkflowForOneProjectOfSeveral() =>
        VerifyWorkflow(new DotNetToolWorkflow(), "My.Tool", directory: "services/api");

    [Fact]
    public Task WritesTheWorkflowForARepositoryThatShipsNothing() => VerifyWorkflow(new DotNetWorkflow(), "My.App", directory: null);

    [Fact]
    public void LeavesTheJobsSomebodyRunsByHandToThem()
    {
        // status, build, install, prepare and init are asked for; scaffolding them would be noise.
        ActionsWorkflowTemplate.Automated(new DotNetToolWorkflow()).Select(job => job.Name).ShouldBe(["check", "deploy"]);
    }

    private static Task VerifyWorkflow(IWorkflow workflow, string name, string? directory)
    {
        // Composed exactly as the step composes it: an empty document, then each job's triggers
        // and the job itself.
        var document = ActionsWorkflow.Parse(ActionsWorkflowTemplate.Document(name));
        foreach (var job in ActionsWorkflowTemplate.Automated(workflow))
        {
            foreach (var (trigger, block) in ActionsWorkflowTemplate.Triggers(job))
            {
                document = document.WithTrigger(trigger, block);
            }

            document = document.WithJob(job.Name, ActionsWorkflowTemplate.Job(job, Tool, directory, "global.json"));
        }

        return Verify(document.Text, "yml");
    }
}
