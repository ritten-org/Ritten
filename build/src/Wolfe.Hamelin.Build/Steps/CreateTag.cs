using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Commands;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Create Git Tag")]
public class CreateTag(ILogger<CreateTag> logger, IOptions<BuildOptions> options, IPipelineContext context, ICommandRunner commands) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var projectInfo = context.State.Get<ProjectInfo>() ?? throw new Exception("Project info not found in state.");
        var tag = $"v{projectInfo.Version}";

        var gitTag = Command.Run("git").WithArguments("tag", tag);
        if (!string.IsNullOrEmpty(options.Value.CommitSha))
        {
            gitTag = gitTag.AndArguments(options.Value.CommitSha);
        }

        logger.LogInformation("Creating git tag {Tag}.", tag);
        await commands.Run(gitTag, cancellationToken);

        var gitPush = Command.Run("git").WithArguments("push", "origin", tag);
        await commands.Run(gitPush, cancellationToken);
    }
}
