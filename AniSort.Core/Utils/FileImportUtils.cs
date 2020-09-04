using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AniSort.Core.Models;
using CsvHelper;
using Microsoft.Extensions.Logging;

namespace AniSort.Core.Utils
{
    public class FileImportUtils
    {
        private ILogger<FileImportUtils> logger;

        public FileImportUtils(ILogger<FileImportUtils> logger)
        {
            this.logger = logger;
        }

        public List<FileImportStatus> LoadImportedFiles()
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
                    logger.LogError(ex, "An error occurred when trying to read from existing files CSV");
                    importedFiles = new List<FileImportStatus>();
                }
            }
            else
            {
                importedFiles = new List<FileImportStatus>();
            }

            return importedFiles;
        }

        public void UpdateImportedFiles(List<FileImportStatus> importedFiles)
        {
            try
            {
                using var writer = new StreamWriter(AppPaths.CheckedFilesPath);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                csv.WriteRecords(importedFiles);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred when trying to write to existing files CSV");
            }
        }
    }
}
