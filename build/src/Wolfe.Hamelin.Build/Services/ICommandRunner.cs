namespace Wolfe.Hamelin.Build.Services;

public interface ICommandRunner
{
    /// <summary>
    /// Runs an external command, streaming its output to the log and returning it for inspection.
    /// </summary>
    Task<CommandOutput> Run(string command, string[] arguments, CancellationToken cancellationToken, bool throwOnNonZeroExit = true);
}
