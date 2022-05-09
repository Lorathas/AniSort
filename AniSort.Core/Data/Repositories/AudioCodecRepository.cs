using System;

namespace AniSort.Core.Data.Repositories;

public class AudioCodecRepository : RepositoryBase<AudioCodec, Guid, AniSortContext>, IAudioCodecRepository
{

    /// <inheritdoc />
    public AudioCodecRepository(AniSortContext context) : base(context)
    {
    }
}
