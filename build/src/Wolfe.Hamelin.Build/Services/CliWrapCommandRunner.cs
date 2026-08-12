using System.Text;
using CliWrap;
using CliWrap.EventStream;
using Hamelin;
using Microsoft.Extensions.Logging;

namespace Wolfe.Hamelin.Build.Services;

public class CliWrapCommandRunner(ILogger<CliWrapCommandRunner> logger, IPipelineContext context) : ICommandRunner
{
    public async Task<CommandOutput> Run(string command, string[] arguments, CancellationToken cancellationToken, bool throwOnNonZeroExit = true)
    {
        var cmd = Cli.Wrap(command)
            .WithArguments(arguments)
            .WithWorkingDirectory(context.CurrentDirectory)
            .WithValidation(CommandResultValidation.None);

        logger.LogInformation("Running command: {Command}", cmd);

        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();
        var exitCode = 0;

        await foreach (var cmdEvent in cmd.ListenAsync(cancellationToken))
        {
            switch (cmdEvent)
            {
                case StartedCommandEvent started:
                    logger.LogInformation("Process started; ID: {ProcessId}", started.ProcessId);
                    break;
                case StandardOutputCommandEvent stdOut:
                    logger.LogInformation("{Output}", stdOut.Text);
                    standardOutput.AppendLine(stdOut.Text);
                    break;
                case StandardErrorCommandEvent stdErr:
                    logger.LogError("{Error}", stdErr.Text);
                    standardError.AppendLine(stdErr.Text);
                    break;
                case ExitedCommandEvent exited:
                    logger.LogInformation("Process exited; Code: {ExitCode}", exited.ExitCode);
                    exitCode = exited.ExitCode;
                    break;
            }
        }

        if (throwOnNonZeroExit && exitCode != 0)
        {
            throw new Exception($"Command {command} returned exit code {exitCode}");
        }

        return new CommandOutput(exitCode, standardOutput.ToString(), standardError.ToString());
    }
}
