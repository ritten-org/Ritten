using System.ComponentModel;
using Hamelin;
using Wolfe.Hamelin.Commands;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Restore Dependencies")]
public class Restore(ICommandRunner commands) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var dotnetRestore = Command.Create("dotnet").WithArguments("restore").ThrowOnError();
        await commands.Run(dotnetRestore, cancellationToken);
    }
}
