using Microsoft.Extensions.Options;
using Ritten.Commands;
using Ritten.Contracts;

namespace Ritten.OnePassword;

/// <summary>
/// Reads secrets through the 1Password CLI by <c>op://vault/item/field</c> reference.
/// </summary>
/// <param name="commands">The command runner.</param>
/// <param name="options">How the CLI authenticates.</param>
internal sealed class OnePasswordSecretProvider(ICommandRunner commands, IOptions<OnePasswordOptions> options) : ISecretProvider
{
    private const string Scheme = "op://";
    private const string TokenVariable = "OP_SERVICE_ACCOUNT_TOKEN";

    /// <inheritdoc />
    public async Task<string> Resolve(string value, CancellationToken ct = default)
    {
        if (!value.StartsWith(Scheme, StringComparison.Ordinal))
        {
            return value;
        }

        // The scheme makes it ours, so a malformed one is refused here rather than handed on as
        // a literal that happens to start with op://.
        var parts = value[Scheme.Length..].Split('/');
        if (parts.Length != 3 || parts.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException($"'{value}' must be {Scheme}<vault>/<item>/<field>.");
        }

        // The reference is safe to log; the value never is, so both streams are redacted.
        var command = Command.Create("op")
            .WithArguments("read", "--no-newline", value)
            .RedactOutput()
            .QuietOutput();
        if (ServiceAccountToken() is { } token)
        {
            command = command.WithEnvironmentVariables(new Dictionary<string, string> { [TokenVariable] = token });
        }

        var result = await commands.Run(command, ct);
        if (result.IsError)
        {
            // Redaction keeps the runner's own failure text to the exit code, which is no help
            // at all. What op writes to stderr names the vault, the item or the account, never
            // the value, and that is the message worth having.
            var why = result.ErrorTail(3);
            throw new CommandFailedException(
                $"Could not read {value}: {(why.Count == 0 ? $"op exited with code {result.ExitCode}" : string.Join(' ', why))}",
                result);
        }

        // op prints the value without a newline; the runner captures output a line at a time
        // and terminates each, so the one line terminator here is the runner's, never the
        // secret's.
        return result.StandardOutput.TrimEnd('\r', '\n');
    }

    private string? ServiceAccountToken()
    {
        if (Environment.GetEnvironmentVariable(TokenVariable) is { Length: > 0 } fromEnvironment)
        {
            return fromEnvironment;
        }

        if (options.Value.ServiceAccountTokenFile is not { Length: > 0 } file)
        {
            return null;
        }

        var path = file.StartsWith("~/", StringComparison.Ordinal)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), file[2..])
            : file;
        return File.Exists(path) && File.ReadAllText(path).Trim() is { Length: > 0 } fromFile ? fromFile : null;
    }
}
