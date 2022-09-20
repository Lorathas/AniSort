namespace AniSort.Core.Models
{
    public enum ImportStatus
    {
        NotYetImported = 0,
        Imported = 1,
        Error = 2,
        NoFileFound = 3,
        ImportedMissingData = 4,
        Hashed = 5,
        Unknown = 6,
    }
}
