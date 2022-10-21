using System;
using System.Collections.Generic;
using System.IO;

namespace AniSort.Core.Extensions;

public static class PathExtensions
{
    public static IEnumerable<string> WalkDirectoryFiles(string path, Func<string, bool>? filter = null)
    {
        if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
        {
            yield return path;
            yield break;
        }
        
        foreach (string file in Directory.GetFiles(path))
        {
            if (filter?.Invoke(file) ?? true)
            {
                yield return file;
            }
        }

        foreach (string subdirectory in Directory.GetDirectories(path))
        {
            foreach (string file in WalkDirectoryFiles(subdirectory, filter))
            {
                yield return file;
            }
        }
    }
}
