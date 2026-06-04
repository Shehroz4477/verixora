// ====================================================================
// VERIXORA – SharedKernel.Domain / Results / Result.cs
// ====================================================================
// Summary:
//   A functional result type for expected failures.  Used throughout
//   the Application layer (CQRS handlers) and the unlock pipeline.
//
//   Design decisions:
//     - Value type (struct) to avoid null references and reduce heap
//       allocations in high‑frequency pipelines.
//     - Private constructor + factory methods guarantee invariants:
//       success has no error, failure has a non‑empty error.
//     - Equality is structural – two results are equal if they have
//       the same success state and the same error/value.
//     - Implicit conversion from T to Result<T> reduces boilerplate
//       in CQRS handlers (return userDto; instead of Success(userDto)).
//
//   Known limitation:
//     The default struct value (default(Result)) has IsSuccess=false
//     and Error=null, which violates the invariant.  Always use the
//     factory methods (Success / Failure) to create results.
// ====================================================================

using System.Diagnostics;

namespace SharedKernel.Domain.Results;

/// <summary>
/// Represents the outcome of a void operation.
/// </summary>
[DebuggerDisplay("{ToString()}")]
public readonly struct Result : IEquatable<Result>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    private Result(bool isSuccess, string error)
    {
        if (isSuccess && !string.IsNullOrEmpty(error))
            throw new InvalidOperationException(
                "A successful result cannot have an error message.");
        if (!isSuccess && string.IsNullOrEmpty(error))
            throw new InvalidOperationException(
                "A failure result must have an error message.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, string.Empty);

    public static Result Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException(
                "Error message cannot be null or whitespace.", nameof(error));
        return new Result(false, error);
    }

    public bool Equals(Result other)
        => IsSuccess == other.IsSuccess &&
           string.Equals(Error, other.Error, StringComparison.Ordinal);

    public override bool Equals(object? obj)
        => obj is Result other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(IsSuccess, Error);

    public static bool operator ==(Result left, Result right) => left.Equals(right);
    public static bool operator !=(Result left, Result right) => !left.Equals(right);

    public override string ToString()
        => IsSuccess ? "Success" : $"Failure: {Error}";
}

/// <summary>
/// Represents the outcome of an operation that returns a value on success.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
[DebuggerDisplay("{ToString()}")]
public readonly struct Result<T> : IEquatable<Result<T>>
{
    private readonly T? _value;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            "Cannot retrieve the value of a failed result.");

    private Result(bool isSuccess, T? value, string error)
    {
        if (isSuccess && !string.IsNullOrEmpty(error))
            throw new InvalidOperationException(
                "A successful result cannot have an error message.");
        if (!isSuccess && string.IsNullOrEmpty(error))
            throw new InvalidOperationException(
                "A failure result must have an error message.");

        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, string.Empty);

    public static Result<T> Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException(
                "Error message cannot be null or whitespace.", nameof(error));
        return new Result<T>(false, default, error);
    }

    public static implicit operator Result<T>(T value) => Success(value);

    public bool Equals(Result<T> other)
        => IsSuccess == other.IsSuccess &&
           string.Equals(Error, other.Error, StringComparison.Ordinal) &&
           EqualityComparer<T?>.Default.Equals(_value, other._value);

    public override bool Equals(object? obj)
        => obj is Result<T> other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(IsSuccess, Error, _value);

    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);
    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

    public override string ToString()
        => IsSuccess ? $"Success: {_value}" : $"Failure: {Error}";
}
