using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Commands;

/// <summary>
/// Runs commands, narrating them to the run's log and recording them as diagnostics.
/// </summary>
/// <param name="log">The run's narrative; silent outside a run.</param>
/// <param name="logger">Where diagnostics go, whatever the host does with them.</param>
/// <param name="fileSystem">The run's file system, whose project root a command runs in unless it names its own
/// directory; outside a run there is none, and commands run in the process's directory.</param>
internal class CommandRunner(IWorkflowLog log, ILogger<CommandRunner> logger, IFileSystem? fileSystem = null) : ICommandRunner
{
    public async Task<CommandResult> Run(Command command, CancellationToken cancellationToken = default)
    {
        var executable = Path.GetFileName(command.Path);
        using var activity = RittenTelemetry.Source.StartActivity(executable);
        activity?.SetTag(RittenTelemetry.Executable, executable);
        try
        {
            var result = await Execute(command, cancellationToken);
            activity?.SetTag(RittenTelemetry.ProcessExitCode, result.ExitCode.Value);
            return result;
        }
        catch (Exception ex) when (activity is not null)
        {
            if (ex is CommandFailedException failed)
            {
                activity.SetTag(RittenTelemetry.ProcessExitCode, failed.Result.ExitCode.Value);
            }

            activity.AddException(ex);
            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private async Task<CommandResult> Execute(Command command, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(fileSystem?.ProjectRoot.AbsolutePath ?? Environment.CurrentDirectory, command.WorkingDirectory ?? string.Empty);
        using var process = new Process();
        process.EnableRaisingEvents = true;
        process.StartInfo = new ProcessStartInfo
        {
            FileName = command.Path,
            WorkingDirectory = directory,
            RedirectStandardInput = command.StandardInput is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in command.Arguments)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        foreach (var (key, value) in command.EnvironmentVariables)
        {
            process.StartInfo.Environment[key] = value;
        }

        // The tool's output is the step's story by default; probes quiet theirs down to --verbose.
        var outputLevel = command.OutputQuieted ? WorkflowLogLevel.Verbose : WorkflowLogLevel.Detail;

        var stdOut = new StringBuilder();
        var stdOutDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        process.OutputDataReceived += CaptureOutput(stdOut, command.OutputRedacted, outputLevel, stdOutDone);

        var stdErr = new StringBuilder();
        var stdErrDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        process.ErrorDataReceived += CaptureOutput(stdErr, command.OutputRedacted, outputLevel, stdErrDone);

        var exitTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        process.Exited += (_, _) => exitTcs.TrySetResult();

        if (!command.ArgumentsRedacted)
        {
            log.Verbose($"Running `{command.Path} {string.Join(" ", command.Arguments)}`");
        }

        // The executable and where it ran, never the arguments: they can carry secrets.
        logger.LogDebug("Running {Executable} in {Directory}.", Path.GetFileName(command.Path), directory);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (command.StandardInput is not null)
        {
            await process.StandardInput.WriteAsync(command.StandardInput.AsMemory(), cancellationToken);
            process.StandardInput.Close();
        }

        await using var _ = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch
            {
                // Process may have exited between the check and Kill; ignore.
            }
        });

        await exitTcs.Task.WaitAsync(cancellationToken);

        await Task.WhenAny(
            Task.WhenAll(stdOutDone.Task, stdErrDone.Task),
            Task.Delay(TimeSpan.FromSeconds(2), CancellationToken.None)
        );

        var exitLogLevel = process.ExitCode == 0 ? WorkflowLogLevel.Verbose : WorkflowLogLevel.Detail;
        log.Log(exitLogLevel, $"Exit code: {process.ExitCode}");
        logger.LogDebug("{Executable} exited with code {ExitCode}.", Path.GetFileName(command.Path), process.ExitCode);

        var result = new CommandResult(process.ExitCode, stdOut.ToString(), stdErr.ToString());
        if (command.ThrowsOnError && result.IsError)
        {
            logger.LogWarning("{Executable} failed with exit code {ExitCode}.", Path.GetFileName(command.Path), result.ExitCode.Value);
            throw new CommandFailedException(FailureMessage(command, result), result);
        }

        return result;
    }

    private static string FailureMessage(Command command, CommandResult result)
    {
        var message = $"Command '{command.Path}' exited with code {result.ExitCode}.";
        if (command.OutputRedacted)
        {
            return message;
        }

        var tail = result.ErrorTail();
        return tail.Count == 0 ? message : $"{message}\n{string.Join('\n', tail)}";
    }

    private DataReceivedEventHandler CaptureOutput(StringBuilder sb, bool hide, WorkflowLogLevel level, TaskCompletionSource tcs) => (_, e) =>
    {
        if (e.Data is null)
        {
            tcs.TrySetResult();
            return;
        }

        sb.AppendLine(e.Data);
        if (!hide)
        {
            log.Log(level, e.Data);
        }
    };
}
