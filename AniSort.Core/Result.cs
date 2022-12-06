using System;

namespace AniSort.Core;

public enum Results
{
    Ok,
    Error
}

public abstract record Result(Results Status)
{
    public string ErrorValue => ((Error) this).Message;

    public static bool IsOk(Result result) => result is Ok;
    public static bool IsError(Result result) => result is Error;
}

public record Ok() : Result(Results.Ok);

public record Error(string Message) : Result(Results.Error);

public abstract record Result<T>(Results Status) where T : notnull
{
    public T OkValue => ((Ok<T>) this).Value;
    public string ErrorValue => ((Error<T>) this).Message;
    
    public static bool IsOk(Result<T> result) => result is Ok<T>;
    public static bool IsError(Result<T> result) => result is Error<T>;
}

public record Ok<T>(T Value) : Result<T>(Results.Ok) where T : notnull;

public record Error<T>(string Message) : Result<T>(Results.Error);

public abstract record Result<TValue, TError>(Results Status)
{
    public TValue OkValue => ((Ok<TValue, TError>) this).Value;
    public TError ErrorValue => ((Error<TValue, TError>) this).Message;
}

public record Ok<TValue, TError>(TValue Value) : Result<TValue, TError>(Results.Ok) where TValue : notnull;

public record Error<TValue, TError>(TError Message) : Result<TValue, TError>(Results.Error);

// public record Result<TResult, TError>(TResult? Value, TError? Error, Results Status)
// {
//     public bool Ok => Status == Results.Ok;
//     public bool Failed => Status == Results.Error;
// }
//
// public record Result<TResult>(TResult? Value, string? Error, Results Status)
// {
//     public bool Ok => Status == Results.Ok;
//     public bool Failed => Status == Results.Error;
// }