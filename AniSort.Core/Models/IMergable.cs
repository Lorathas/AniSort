using System;

namespace AniSort.Core.Models;

/// <summary>
/// Interface for objects that are able to merge with another instance
/// </summary>
public interface IMergable<TType, out TKey>
    where TType : IMergable<TType, TKey>
    where TKey : IComparable
{
    /// <summary>
    /// Merge object with other object
    /// Will prefer values in calling object over other object
    /// </summary>
    /// <param name="other">Object to merge with</param>
    /// <returns>New object with the two merged together</returns>
    public TType MergeWith(TType other);
    
    /// <summary>
    /// Getter for key that determines mergability
    /// </summary>
    public TKey Key { get; }

    /// <summary>
    /// Check if object is semantically mergable with another object based on the key
    /// </summary>
    /// <param name="other">Object to check mergability with</param>
    /// <returns></returns>
    public bool IsMergeableWith(TType other);
}
