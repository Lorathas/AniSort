namespace AniSort.Core.Data;

public enum JobType
{
    SortFile = 0,
    SortDirectory = 1,
    HashFile = 2,
    HashDirectory = 3,
    Defragment = 4,
    Maintenance = 5,
}