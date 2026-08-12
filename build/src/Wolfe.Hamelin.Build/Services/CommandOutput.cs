namespace Wolfe.Hamelin.Build.Services;

public record CommandOutput(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Success => ExitCode == 0;
}
