using System.Text;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;
using Ritten.Engine;

namespace Ritten.Commands;

/// <summary>
/// An env file, the way <c>docker compose --env-file</c> or <c>op run --env-file</c> read one:
/// <c>NAME=value</c> per line, comments and blanks ignored, quotes around a value dropped.
/// </summary>
/// <remarks>
/// Each value goes through the registered <see cref="ISecretProvider"/>: a reference comes back read, a
/// literal as written. The file on disk therefore never holds a secret; it
/// holds the name of one, and a command runs under the value.
/// </remarks>
public static class EnvironmentFile
{
    /// <summary>
    /// Reads the file's entries as written, a name given twice its later value; a name that is no variable's is an
    /// error at its line.
    /// </summary>
    /// <param name="text">The file's contents.</param>
    /// <param name="fileName">The file, for the error.</param>
    public static Result<EnvironmentVariables> Parse(string text, string fileName)
    {
        var entries = EnvironmentVariables.Empty;
        var errors = new List<Error>();
        foreach (var (line, number) in text.Split('\n').Select((line, index) => (line.Trim(), index + 1)))
        {
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var assignment = line.StartsWith("export ", StringComparison.Ordinal) ? line["export ".Length..].TrimStart() : line;
            var separator = assignment.IndexOf('=');
            if (separator <= 0)
            {
                errors.Add(new Error($"{fileName}:{number}: expected NAME=value."));
                continue;
            }

            var name = EnvironmentVariableName.Parse(assignment[..separator].Trim());
            if (name.IsError)
            {
                errors.AddRange(name.Errors.Select(error => new Error($"{fileName}:{number}: {error.Message}")));
                continue;
            }

            entries = entries.With(name.Value, Unquote(assignment[(separator + 1)..].Trim()));
        }

        return errors.Count > 0 ? errors : entries;
    }

    /// <summary>
    /// Loads the file and resolves its references: literals as written, references through the store.
    /// </summary>
    /// <param name="file">The env file.</param>
    /// <param name="secretProvider">The secrets store the host registered.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public static async Task<Result<EnvironmentVariables>> Load(IFile file, ISecretProvider secretProvider, CancellationToken cancellationToken = default)
    {
        if (!file.Exists)
        {
            return new Error($"{file.AbsolutePath} does not exist.");
        }

        using var reader = new StreamReader(file.OpenRead(), Encoding.UTF8);
        var entries = Parse(await reader.ReadToEndAsync(cancellationToken), file.Name);
        if (entries.IsError)
        {
            return entries.Errors;
        }

        var variables = EnvironmentVariables.Empty;
        foreach (var (name, value) in entries.Value)
        {
            variables = variables.With(name, await secretProvider.Resolve(value, cancellationToken));
        }

        return variables;
    }

    /// <summary>
    /// Loads several files in order, a later file's entry replacing an earlier one's.
    /// </summary>
    /// <param name="files">The env files.</param>
    /// <param name="secretProvider">The secrets store the host registered.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public static async Task<Result<EnvironmentVariables>> Load(IEnumerable<IFile> files, ISecretProvider secretProvider, CancellationToken cancellationToken = default)
    {
        var variables = EnvironmentVariables.Empty;
        foreach (var file in files)
        {
            var loaded = await Load(file, secretProvider, cancellationToken);
            if (loaded.IsError)
            {
                return loaded.Errors;
            }

            variables = variables.Merge(loaded.Value);
        }

        return variables;
    }

    private static string Unquote(string value) =>
        value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0] ? value[1..^1] : value;
}
