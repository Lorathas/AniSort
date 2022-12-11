using System;
using AniSort.Core.Data.Repositories.EF;

namespace AniSort.Core.Data.Repositories;

public class AudioCodecRepository : RepositoryBase<AudioCodec, Guid, AniSortContext>, IAudioCodecRepository
{

    /// <inheritdoc />
    public AudioCodecRepository(AniSortContext context) : base(context)
    {
    }
}
