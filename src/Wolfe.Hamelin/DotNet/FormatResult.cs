namespace Wolfe.Hamelin.DotNet;

/// <summary>
/// The outcome of a <see cref="IDotNet.CheckFormat"/> invocation.
/// </summary>
public record FormatResult
{
    /// <summary>
    /// True if every file is correctly formatted, otherwise false.
    /// </summary>
    public required bool Succeeded { get; init; }

    /// <summary>
    /// The files that aren't formatted, as paths relative to the pipeline's current directory.
    /// Can be empty even for a failed check if <c>dotnet format</c> failed before producing a report.
    /// </summary>
    public IReadOnlyList<string> UnformattedFiles { get; init; } = [];
}
