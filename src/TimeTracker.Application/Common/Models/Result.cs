namespace TimeTracker.Application.Common.Models;

/// <summary>
/// A lightweight operation-result wrapper so handlers can communicate business-rule
/// failures (e.g. "not found", "forbidden") without relying on exceptions for control flow.
/// </summary>
public class Result
{
    public bool Succeeded { get; }
    public string? Error { get; }
    public ResultErrorType ErrorType { get; }

    protected Result(bool succeeded, string? error, ResultErrorType errorType)
    {
        Succeeded = succeeded;
        Error = error;
        ErrorType = errorType;
    }

    public static Result Success() => new(true, null, ResultErrorType.None);

    public static Result Failure(string error, ResultErrorType errorType = ResultErrorType.Validation) =>
        new(false, error, errorType);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool succeeded, T? value, string? error, ResultErrorType errorType)
        : base(succeeded, error, errorType)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null, ResultErrorType.None);

    public static new Result<T> Failure(string error, ResultErrorType errorType = ResultErrorType.Validation) =>
        new(false, default, error, errorType);
}

public enum ResultErrorType
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Forbidden = 3,
    Conflict = 4
}
