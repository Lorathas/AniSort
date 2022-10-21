using System;
using System.Threading.Tasks;

namespace AniSort.Core.Helpers;

public static class ResultHelpers
{
    public static Ok<T> Ok<T>(this T value) where T : notnull => new(value);
    
    public static Error<T> Error<T>(this string message) => new(message);

    public static Ok<TResult, TError> Ok<TResult, TError>(this TResult value) where TResult : notnull => new(value);
    
    public static Error<TResult, TError> Error<TResult, TError>(this TError error) => new(error);

    public static Action<Result<T>> Iter<T>(this Action<T> action) where T : notnull
    {
        return r =>
        {
            if (r is Ok<T> ok)
            {
                action(ok.Value);
            }
        };
    }

    public static Func<Result<T>, Result<T>> Map<T>(this Func<T, Result<T>> mapper) where T : notnull
    {
        return r =>
        {
            return r switch
            {
                Ok<T> ok => mapper(ok.Value),
                Error<T> e => e,
                _ => throw new ArgumentOutOfRangeException(nameof(r), r, null)
            };
        };
    }
    
    public static Func<Result<T>, Task<Result<T>>> MapAsync<T>(this Func<T, Task<Result<T>>> mapper) where T : notnull
    {
        return r =>
        {
            return r switch
            {
                Ok<T> ok => mapper(ok.Value),
                Error<T> e => Task.FromResult((Result<T>) e),
                _ => throw new ArgumentOutOfRangeException(nameof(r), r, null)
            };
        };
    }
    
    public static Func<Result<T1>, Result<T2>> Map<T1, T2>(this Func<T1, Result<T2>> mapper) where T1 : notnull where T2 : notnull
    {
        return r =>
        {
            return r switch
            {
                Ok<T1> ok => mapper(ok.Value),
                Error<T1> e => new Error<T2>(e.Message),
                _ => throw new ArgumentOutOfRangeException(nameof(r), r, null)
            };
        };
    }
    
    public static Func<Result<T1>, Task<Result<T2>>> MapAsync<T1, T2>(this Func<T1, Task<Result<T2>>> mapper) where T1 : notnull where T2 : notnull
    {
        return r =>
        {
            return r switch
            {
                Ok<T1> ok => mapper(ok.Value),
                Error<T1> e => Task.FromResult((Result<T2>) new Error<T2>(e.Message)),
                _ => throw new ArgumentOutOfRangeException(nameof(r), r, null)
            };
        };
    }
    
    public static Action<Result<T>> IterError<T>(this Action<string> action) where T : notnull
    {
        return r =>
        {
            if (r is Error<T> error) action(error.Message);
        };
    }
    
    public static Func<Result<T>, Result<T>> MapError<T>(this Func<string, Result<T>> mapper) where T : notnull
    {
        return r =>
        {
            return r switch
            {
                Ok<T> ok => ok,
                Error<T> e => mapper(e.Message),
                _ => throw new ArgumentOutOfRangeException(nameof(r), r, null)
            };
        };
    }
}