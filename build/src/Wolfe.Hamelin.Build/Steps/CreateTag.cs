using System.ComponentModel;
using Hamelin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Commands;
using Wolfe.Hamelin.DotNet;

namespace Wolfe.Hamelin.Build.Steps;

[DisplayName("Create Git Tag")]
public class CreateTag(ILogger<CreateTag> logger, IOptions<BuildOptions> options, IPipelineContext context, ICommandRunner commands) : IPipelineStep
{
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var project = context.State.Get<Project>() ?? throw new Exception("Project info not found in state.");
        var tag = $"v{project.Version}";

        var gitTag = Command.Create("git").WithArguments("tag", tag).ThrowOnError();
        if (!string.IsNullOrEmpty(options.Value.CommitSha))
        {
            gitTag = gitTag.AndArguments(options.Value.CommitSha);
        }

        logger.LogInformation("Creating git tag {Tag}.", tag);
        await commands.Run(gitTag, cancellationToken);

        var gitPush = Command.Create("git").WithArguments("push", "origin", tag).ThrowOnError();
        await commands.Run(gitPush, cancellationToken);
    }
}
