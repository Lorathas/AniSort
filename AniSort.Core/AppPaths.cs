using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AniSort.Core
{
    public static class AppPaths
    {
        private static string dataPath;

        /// <summary>
        /// Path to the default data path (roaming app data/home) folder to store data in
        /// </summary>
        public static string DataPath
        {
            get
            {
                if (dataPath == null)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        string appData = Environment.GetEnvironmentVariable("AppData");

                        if (appData == null)
                        {
                            throw new Exception("No %APPDATA% environment variable found. Please check that you have one set. If this keeps happening please contact the developer.");
                        }

                        dataPath = Path.Combine(appData, "AniSort");
                    }
                    else
                    {
                        string home = Environment.GetEnvironmentVariable("HOME");

                        if (home == null)
                        {
                            throw new Exception("No HOME environment variable found. Please check that it is set. If this keeps happening please contact the developer.");
                        }

                        dataPath = Path.Combine(home, ".anisort");
                    }
                }

                return dataPath;
            }
        }

        public static string CheckedFilesPath => Path.Combine(DataPath, "checked-files.csv");

        /// <summary>
        /// Path to store the anime title dump from 
        /// </summary>
        public static string AnimeTitlesDumpPath => Path.Combine(DataPath, "anime-titles.dat.gz");

        /// <summary>
        /// Path that the database should be stored at
        /// </summary>
        public static string DatabasePath => Path.Combine(DataPath, "anisort.sqlite3");

        public static void Initialize()
        {
            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }
        }

    }
}
