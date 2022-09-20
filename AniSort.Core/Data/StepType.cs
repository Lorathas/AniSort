namespace AniSort.Core.Data;

public enum StepType
{
    DiscoverFiles,
    FetchLocalFile,
    Hash,
    FetchMetadata,
    Sort,
    DiscoverFragmentedSeries,
    DetermineDefragmentFolder,
    DefragmentSeries,
}
