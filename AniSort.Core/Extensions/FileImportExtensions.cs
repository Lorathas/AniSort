using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks.Dataflow;

namespace AniSort.Core.Extensions;

public static class FileImportExtensions
{
    public static readonly string[] SupportedFileExtensions =
    {
        ".mkv", ".mp4", ".avi", ".wmv", ".mov", ".mpeg", ".mpg", ".flv", ".webm", ".ogg", ".vob", ".m2ts", ".mts",
        ".ts", ".yuv", ".rm", ".rmvb", ".m4v", ".m4p", ".mp2", ".mpe", ".mpv", ".m2v", ".svi", ".3gp", ".3g2",
        ".mxf", ".nsv", ".f4v", ".f4p", ".f4a", ".f4b"
    };
        
    /// <summary>
    /// Recursively add paths to a queue
    /// </summary>
    /// <param name="queue">Queue to add to</param>
    /// <param name="paths">Top level paths to add</param>
    /// <param name="predicate">Optional filtering predicate</param>
    public static void AddPathsToQueue(this Queue<string> queue, IEnumerable<string?> paths, Func<string, bool>? predicate = null)
    {
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                queue.AddPathsToQueue(Directory.GetFiles(path).Concat(Directory.GetDirectories(path)), predicate);
            }
            else if (SupportedFileExtensions.Contains(Path.GetExtension(path)) && File.Exists(path) && (predicate == null || predicate(path)))
            {
                queue.Enqueue(path);
            }
        }
    }

    /// <summary>
    /// Recursively add paths to a queue
    /// </summary>
    /// <param name="queue">Queue to add to</param>
    /// <param name="path">Top level path to add</param>
    /// <param name="predicate">Optional filtering predicate</param>
    public static void AddPathsToQueue(this Queue<string> queue, string? path, Func<string, bool>? predicate = null) =>
        queue.AddPathsToQueue(new[] { path }, predicate);

    /// <summary>
    /// Recursively post paths to a dataflow block
    /// </summary>
    /// <param name="block">Dataflow block to add to</param>
    /// <param name="paths">Top level paths to add</param>
    /// <param name="predicate">Optional filtering predicate</param>
    public static void AddPathsToFlow(this ITargetBlock<string> block, IEnumerable<string?> paths, Func<string, bool>? predicate = null)
    {
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                block.AddPathsToFlow(Directory.GetFiles(path).Concat(Directory.GetDirectories(path)), predicate);
            }
            else if (SupportedFileExtensions.Contains(Path.GetExtension(path)) && File.Exists(path) && (predicate == null || predicate(path)))
            {
                block.Post(path);
            }
        }
    }

    /// <summary>
    /// Recursively post paths to a List
    /// </summary>
    /// <param name="files">List to add to</param>
    /// <param name="paths">Top level paths to add</param>
    /// <param name="predicate">Optional filtering predicate</param>
    public static void AddPathsToList(this List<string> files, IEnumerable<string> paths, Func<string, bool>? predicate = null)
    {
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                files.AddPathsToList(Directory.GetFiles(path).Concat(Directory.GetDirectories(path)), predicate);
            }
            else if (SupportedFileExtensions.Contains(Path.GetExtension(path)) && File.Exists(path) && (predicate == null || predicate(path)))
            {
                files.Add(path);
            }
        }
    }

    /// <summary>
    /// Recursively post paths to a List
    /// </summary>
    /// <param name="files">List to add to</param>
    /// <param name="path">Top level path to add</param>
    /// <param name="predicate">Optional filtering predicate</param>
    public static void AddPathsToList(this List<string> files, string path, Func<string, bool>? predicate = null) => files.AddPathsToList(new[]
    {
        path
    }, predicate);
}
