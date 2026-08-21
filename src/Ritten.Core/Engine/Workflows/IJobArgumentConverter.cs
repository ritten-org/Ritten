namespace Ritten.Engine.Workflows;

/// <summary>
/// Maps a job's argument declarations onto whatever a front end needs them to be.
/// </summary>
/// <typeparam name="TResult">What each declaration maps to.</typeparam>
public interface IJobArgumentConverter<out TResult>
{
    /// <summary>
    /// Maps one declaration, recovering the type it reads as.
    /// </summary>
    /// <typeparam name="T">The type the argument reads as.</typeparam>
    /// <param name="argument">The declaration to map.</param>
    TResult Convert<T>(JobArgument<T> argument);
}
