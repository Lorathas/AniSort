using System;

namespace AniSort.Core.Models;

public record EpisodeNumber(EpisodeType Type, int Number) : IComparable
{

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (obj is EpisodeNumber other)
        {
            int typeComparison = Type.CompareTo(other.Type);

            return typeComparison != 0 ? typeComparison : Number.CompareTo(other.Number);
        }

        return 1;
    }
}
