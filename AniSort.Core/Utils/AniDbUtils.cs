using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AniSort.Core.Utils
{
    public static class AniDbUtils
    {
        public const string AnimeTitlesUrl = "http://anidb.net/api/anime-titles.dat.gz";

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if the file was updated and should be examined for changes</returns>
        public static async Task<bool> UpdateAnimeTitlesDumpIfNeededAsync()
        {
            bool shouldDownload = false;

            if (!File.Exists(AppPaths.AnimeTitlesDumpPath))
            {
                if (!Directory.Exists(AppPaths.DataPath))
                {
                    Directory.CreateDirectory(AppPaths.DataPath);
                }

                shouldDownload = true;
            }
            else if (DateTime.Now.Subtract(File.GetLastWriteTime(AppPaths.AnimeTitlesDumpPath)) > TimeSpan.FromDays(1))
            {
                shouldDownload = true;
            }

            if (!shouldDownload)
            {
                return false;
            }

            using var client = new HttpClient();
            await using var stream = await client.GetStreamAsync(AnimeTitlesUrl);
            await using var fs = File.Open(AppPaths.AnimeTitlesDumpPath, FileMode.Create);

            await stream.CopyToAsync(fs);

            return true;
        }

        public static async Task<int> UpdateAnimeTitlesInDatabase()
        {
            throw new NotImplementedException();
        }
    }
}
