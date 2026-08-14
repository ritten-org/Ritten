using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Ritten.Core;

/// <summary>
/// Creates <see cref="Result{T}"/> values.
/// </summary>
public static class Result
{
    /// <summary>
    /// Creates a successful result carrying the given value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value the operation produced.</param>
    public static Result<T> Success<T>(T value) where T : class => new(value);

    /// <summary>
    /// Creates a failed result carrying the given errors.
    /// </summary>
    /// <typeparam name="T">The type the operation would have produced.</typeparam>
    /// <param name="errors">Everything that was wrong, not just the first thing.</param>
    public static Result<T> Error<T>(IEnumerable<Error> errors) where T : class => new(errors);

    /// <summary>
    /// Creates a single <see cref="Ritten.Core.Error"/>, which converts implicitly to a failed <see cref="Result{T}"/> of any type.
    /// </summary>
    /// <param name="message">A message describing what was wrong.</param>
    public static Error Error(string message) => new(message);
}

/// <summary>
/// Either a value or the reasons one couldn't be produced.
/// </summary>
/// <typeparam name="T">The type of the value produced on success.</typeparam>
public class Result<T> where T : class
{
    /// <summary>
    /// Creates a successful result carrying the given value.
    /// </summary>
    /// <param name="value">The value the operation produced.</param>
    public Result(T value) : this(value, null) { }

    /// <summary>
    /// Creates a failed result carrying the given errors.
    /// </summary>
    /// <param name="errors">Everything that was wrong, not just the first thing.</param>
    public Result(IEnumerable<Error> errors) : this(null, errors) { }

    private Result(T? value, IEnumerable<Error>? errors)
    {
        Value = value;
        Errors = errors?.ToImmutableList();
    }

    /// <summary>
    /// The value produced, or <c>null</c> if the operation failed.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Everything that went wrong, or <c>null</c> if the operation succeeded.
    /// </summary>
    public IReadOnlyCollection<Error>? Errors { get; }

    /// <summary>
    /// Whether the operation failed, in which case <see cref="Errors"/> says why.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Errors))]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsError => Errors != null;

    /// <summary>
    /// Whether the operation succeeded, in which case <see cref="Value"/> holds its result.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Errors))]
    public bool IsSuccess => !IsError;

    /// <summary>
    /// Converts a value to a successful result, so that a method can simply return it.
    /// </summary>
    /// <param name="value">The value the operation produced.</param>
    public static implicit operator Result<T>(T value) => new(value);

    /// <summary>
    /// Converts a list of errors to a failed result, so that a method can simply return them.
    /// </summary>
    /// <param name="errors">Everything that was wrong.</param>
    public static implicit operator Result<T>(List<Error> errors) => new(errors);

    /// <summary>
    /// Converts a single error to a failed result, so that a method can simply return it.
    /// </summary>
    /// <param name="error">What was wrong.</param>
    public static implicit operator Result<T>(Error error) => new([error]);
}
