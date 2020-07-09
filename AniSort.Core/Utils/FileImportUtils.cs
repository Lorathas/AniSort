using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AniSort.Core.Models;
using CsvHelper;

namespace AniSort.Core.Utils
{
    public static class FileImportUtils
    {
        public static List<FileImportStatus> LoadImportedFiles()
        {
            List<FileImportStatus> importedFiles;

            if (File.Exists(AppPaths.CheckedFilesPath))
            {
                try
                {
                    using var reader = new StreamReader(AppPaths.CheckedFilesPath);
                    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                    importedFiles = csv.GetRecords<FileImportStatus>().ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    importedFiles = new List<FileImportStatus>();
                }
            }
            else
            {
                importedFiles = new List<FileImportStatus>();
            }

            return importedFiles;
        }

        public static void UpdateImportedFiles(List<FileImportStatus> importedFiles)
        {
            try
            {
                using var writer = new StreamWriter(AppPaths.CheckedFilesPath);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                csv.WriteRecords(importedFiles);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
