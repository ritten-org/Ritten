using System.Diagnostics;
using System.Text;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.Commands;

internal class CommandRunner(IWorkflowLog log, IFileSystem fileSystem) : ICommandRunner
{
    public async Task<CommandResult> Run(Command command, CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        process.EnableRaisingEvents = true;
        process.StartInfo = new ProcessStartInfo
        {
            FileName = command.Path,
            WorkingDirectory = Path.Combine(fileSystem.ProjectRoot.AbsolutePath, command.WorkingDirectory ?? string.Empty),
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

        var result = new CommandResult(process.ExitCode, stdOut.ToString(), stdErr.ToString());
        if (command.ThrowsOnError && result.IsError)
        {
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
