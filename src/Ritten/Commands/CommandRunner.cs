using System.Diagnostics;
using System.Text;
using Ritten.Contracts;
using Microsoft.Extensions.Logging;

namespace Ritten.Commands;

internal class CommandRunner(ILogger<CommandRunner> logger, IPipelineContext context) : ICommandRunner
{
    public async Task<CommandResult> Run(Command command, CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        process.EnableRaisingEvents = true;
        process.StartInfo = new ProcessStartInfo
        {
            FileName = command.Path,
            WorkingDirectory = Path.Combine(context.CurrentDirectory, command.WorkingDirectory ?? string.Empty),
            RedirectStandardInput = command.StandardInput is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Add the args.
        foreach (var arg in command.Arguments)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        // Add the env vars
        foreach (var (key, value) in command.EnvironmentVariables)
        {
            process.StartInfo.Environment[key] = value;
        }

        var stdOut = new StringBuilder();
        var stdOutDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        process.OutputDataReceived += LogStandard(stdOut, command.OutputRedacted, LogLevel.Information, stdOutDone);

        var stdErr = new StringBuilder();
        var stdErrDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        process.ErrorDataReceived += LogStandard(stdErr, command.OutputRedacted, command.StandardErrorLogLevel, stdErrDone);

        var exitTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        process.Exited += (_, _) => exitTcs.TrySetResult();

        if (logger.IsEnabled(LogLevel.Information))
        {
            var args = command.ArgumentsRedacted ? "[REDACTED]" : string.Join(" ", command.Arguments);
            logger.LogInformation("Running command: {Command} {Arguments}", command.Path, args);
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

        // Wait on the process exit event only. Do NOT wait for stdout/stderr EOF:
        // on macOS the parent's copy of a pipe write-end can be left open after the
        // child dies (observed with `tofu apply`), so the read end never EOFs and
        // a stream-based wait blocks forever even though the process is gone.
        await exitTcs.Task.WaitAsync(cancellationToken);

        // Best-effort drain so trailing lines aren't lost; bounded so a leaked pipe can't hang.
        await Task.WhenAny(
            Task.WhenAll(stdOutDone.Task, stdErrDone.Task),
            Task.Delay(TimeSpan.FromSeconds(2), CancellationToken.None)
        );

        logger.LogInformation("Exit code: {ExitCode}", process.ExitCode);
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

        var tail = result.StandardError
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .TakeLast(10)
            .ToList();
        return tail.Count == 0 ? message : $"{message}\n{string.Join('\n', tail)}";
    }

    private DataReceivedEventHandler LogStandard(StringBuilder sb, bool hide, LogLevel logLevel, TaskCompletionSource tcs) => (_, e) =>
    {
        if (e.Data is null)
        {
            tcs.TrySetResult();
            return;
        }

        sb.AppendLine(e.Data);
        if (!hide && logger.IsEnabled(logLevel))
        {
            logger.Log(logLevel, "{Value}", e.Data);
        }
    };
}
