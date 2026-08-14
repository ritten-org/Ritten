using System.Diagnostics.CodeAnalysis;

namespace Ritten.Core;

public static class Result
{
    public static Result<T> Success<T>(T value) => new(value);
    public static Result<T> Error<T>(List<Error> errors) => new(errors);

    public static Error Error(string message) => new(message);
}

public class Result<T>
{
    public Result(T value) : this (value, null) { }
    public Result(List<Error> errors) : this(default, errors) { }
    private Result(T? value, List<Error>? errors)
    {
        Value = value;
        Errors = errors;
    }

    public T? Value { get; }
    public List<Error>? Errors { get; }

    [MemberNotNullWhen(true, nameof(Errors))]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsError => Errors != null;

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Errors))]
    public bool IsSuccess => !IsError;

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(List<Error> errors) => new(errors);
    public static implicit operator Result<T>(Error error) => new([error]);
}

public record Error(string Message)
{
    public static implicit operator Error(string message) => new(message);
};
