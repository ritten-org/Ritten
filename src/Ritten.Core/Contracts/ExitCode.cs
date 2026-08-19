namespace Ritten.Contracts;

/// <summary>
/// A process exit code with its meaning attached.
/// </summary>
/// <param name="Value">The numeric code the process boundary sees.</param>
public readonly record struct ExitCode(int Value)
{
    /// <summary>
    /// Everything completed successfully.
    /// </summary>
    public static readonly ExitCode Success = new(0);

    /// <summary>
    /// A step failed.
    /// </summary>
    public static readonly ExitCode Failed = new(1);

    /// <summary>
    /// The workflow never started because its configuration is invalid.
    /// </summary>
    public static readonly ExitCode ConfigurationError = new(2);

    /// <summary>
    /// The run was cancelled. Follows the shell convention of 128 + SIGINT.
    /// </summary>
    public static readonly ExitCode Cancelled = new(130);

    /// <summary>
    /// Whether this code reports success.
    /// </summary>
    public bool IsSuccess => Value == 0;

    /// <summary>
    /// Presents the code as the process boundary wants it.
    /// </summary>
    public static implicit operator int(ExitCode code) => code.Value;

    /// <summary>
    /// Adopts a raw code from a process boundary.
    /// </summary>
    public static implicit operator ExitCode(int value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
