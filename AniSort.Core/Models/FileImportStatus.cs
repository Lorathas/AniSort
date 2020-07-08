namespace AniSort.Core.Models
{
    public class FileImportStatus
    {
        public string FilePath { get; set; }
        public ImportStatus Status { get; set; }
        public string Message { get; set; }
        public byte[] Hash { get; set; }
        public long FileLength { get; set; }
        public int Attempts { get; set; }

        public FileImportStatus()
        {
        }

        public FileImportStatus(string filePath)
        {
            FilePath = filePath;
        }
    }
}
