using System.ComponentModel;
using Hamelin;
using Wolfe.Hamelin.Commands;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Restore Dependencies")]
public class Restore(ICommandRunner commands) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var dotnetRestore = Command.Run("dotnet").WithArguments("restore");
        await commands.Run(dotnetRestore, cancellationToken);
    }
}
