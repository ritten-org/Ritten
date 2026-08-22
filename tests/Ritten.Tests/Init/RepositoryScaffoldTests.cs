using System.Text;
using Ritten.Engine.Workflows;
using Ritten.Init;
using Ritten.Workflows.DotNet;
using Ritten.Workflows.DotNetPackage;
using Ritten.Workflows.DotNetTool;

namespace Ritten.Tests.Init;

/// <summary>
/// The whole of what a repository is handed, as the files themselves rather than as assertions
/// about them. These snapshots are here to be read: the derivation is only worth trusting if you
/// can see what it produces.
/// </summary>
public class RepositoryScaffoldTests
{
    [Fact]
    public Task ScaffoldsARepositoryThatShipsATool() => VerifyScaffold(new DotNetToolWorkflow());

    [Fact]
    public Task ScaffoldsARepositoryThatShipsAPackage() => VerifyScaffold(new DotNetPackageWorkflow());

    [Fact]
    public Task ScaffoldsARepositoryThatShipsNothing() => VerifyScaffold(new DotNetWorkflow());

    private static Task VerifyScaffold(IWorkflow workflow)
    {
        // A fixed version, so a snapshot doesn't move every time Ritten is released.
        var files = RepositoryScaffold.For(workflow, "src/My.Tool/My.Tool.csproj", "1.2.3", "ritten.json");

        var document = new StringBuilder();
        foreach (var file in files)
        {
            document
                .Append("──────── ").Append(file.Path)
                .Append(file.Generated ? " (generated)" : " (seed)").Append('\n')
                .Append(file.Content)
                .Append('\n');
        }

        return Verify(document.ToString());
    }
}
