using System;

namespace Desktop.Results;

public class EmptyResult<TError> : Result<object, TError> where TError : Error
{
    protected EmptyResult() : base(value: null)
    {
    }

    protected EmptyResult(TError error) : base(error)
    {
    }

    public static EmptyResult<TError> SuccessFull => new();

    public static implicit operator EmptyResult<TError>(TError error)
    {
        return new EmptyResult<TError>(error);
    }
}

public class Result<TValue> : Result<TValue, Error> where TValue : class
{
    protected Result(TValue value) : base(value)
    {
    }

    protected Result(Error error) : base(error)
    {
    }

    public static implicit operator Result<TValue>(TValue value)
    {
        return new Result<TValue>(value);
    }

    public static implicit operator Result<TValue>(Error error)
    {
        return new Result<TValue>(error);
    }
}

public class Result<TValue, TError> where TError : Error where TValue : class
{
    private readonly TValue? _value;

    protected Result(TValue value)
    {
        _value = value;
        Success = true;
    }

    protected Result(TError error)
    {
        _value = default;
        Error = error;
        Success = false;
    }

    protected Result(TError error, TValue partialValue)
    {
        _value = partialValue;
        Error = error;
        Success = false;
    }

    public bool Success { get; }

    public TValue Value => Success
        ? _value!
        : throw new InvalidOperationException("The Result was not successful, so there is no Value");

    public TValue? PartialValue => Success
        ? throw new InvalidOperationException("The Result was successful, so there is no PartialValue")
        : _value;

    public TError Error
    {
        get => Success ? throw new InvalidOperationException("The Result was successful, so there is no Error") : field;
    } = null!;

    public static implicit operator Result<TValue, TError>(TValue value)
    {
        return new Result<TValue, TError>(value);
    }

    public static implicit operator Result<TValue, TError>(TError error)
    {
        return new Result<TValue, TError>(error);
    }

    public static Result<TValue, TError> CreatePartialResult(TValue partialValue, TError error)
    {
        return new Result<TValue, TError>(error, partialValue);
    }
}