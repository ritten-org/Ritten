using System.ComponentModel;
using Hamelin;
using Wolfe.Hamelin.Build.Services;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Restore Dependencies")]
public class Restore(ICommandRunner commands) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        await commands.Run(
            command: "dotnet",
            arguments: ["restore"],
            cancellationToken
        );
    }
}
