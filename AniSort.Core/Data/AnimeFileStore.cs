using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AniSort.Core.Models;
using Microsoft.Extensions.Logging;
using FileInfo = AniSort.Core.Models.FileInfo;

namespace AniSort.Core.Data;

public class AnimeFileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };
    
    public ConcurrentDictionary<int, AnimeInfo> Anime { get; private set; } = new();
    public ConcurrentDictionary<byte[], FileInfo> Files { get; private set; } = new();
    public SemaphoreSlim WriteLock { get; } = new(1, 1);

    private bool initialized = false;
    
    public ILogger<AnimeFileStore> Logger { get; set; }

    public AnimeFileStore()
    {
    }

    public AnimeFileStore(IEnumerable<AnimeInfo> anime)
    {
        foreach (var item in anime)
        {
            Anime[item.Id] = item;
        }
    }

    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        WriteLock.Wait();
        try
        {
            Logger?.LogTrace("Initializing anime file store");

            var stopwatch = Stopwatch.StartNew();
            
            using var fs = File.OpenRead(AppPaths.AnimeInfoFilePath);
            Anime = JsonSerializer.Deserialize<ConcurrentDictionary<int, AnimeInfo>>(fs) ?? new();

            foreach (var (_, anime) in Anime)
            {
                for (int idx = 0; idx < anime.Episodes.Count; idx++)
                {
                    var episode = anime.Episodes[idx] = anime.Episodes[idx] with { Anime = anime };

                    for (int jdx = 0; jdx < episode.Files.Count; jdx++)
                    {
                        var file = episode.Files[jdx] with { Episode = episode };

                        if (file.Ed2kHash == null)
                        {
                            continue;
                        }
                        
                        Files[file.Ed2kHash] = file;
                    }
                }
            }
            stopwatch.Stop();
            
            Logger?.LogTrace("Initialized anime file store in {ElapsedTime}", stopwatch.Elapsed);
        }
        finally
        {
            WriteLock.Release();
        }
    }

    public async Task InitializeAsync()
    {
        if (initialized)
        {
            return;
        }

        await WriteLock.WaitAsync();
        try
        {
            Logger?.LogTrace("Initializing anime file store");

            var stopwatch = Stopwatch.StartNew();
            
            await using var fs = File.OpenRead(AppPaths.AnimeInfoFilePath);
            Anime = JsonSerializer.Deserialize<ConcurrentDictionary<int, AnimeInfo>>(fs) ?? new();

            foreach (var (_, anime) in Anime)
            {
                for (int idx = 0; idx < anime.Episodes.Count; idx++)
                {
                    var episode = anime.Episodes[idx] = anime.Episodes[idx] with { Anime = anime };

                    for (int jdx = 0; jdx < episode.Files.Count; jdx++)
                    {
                        var file = episode.Files[jdx] with { Episode = episode };

                        if (file.Ed2kHash == null)
                        {
                            continue;
                        }
                        
                        Files[file.Ed2kHash] = file;
                    }
                }
            }
            stopwatch.Stop();
            
            Logger?.LogTrace("Initialized anime file store in {ElapsedTime}", stopwatch.Elapsed);
        }
        finally
        {
            WriteLock.Release();
        }
    }

    public void Save()
    {
        WriteLock.Wait();

        try
        {

            Logger?.LogTrace("Writing anime file store to disk");
            var stopwatch = Stopwatch.StartNew();

            using var fs = File.OpenWrite(AppPaths.AnimeInfoFilePath);
            JsonSerializer.Serialize(fs, Anime, SerializerOptions);
            fs.Flush();

            Logger?.LogTrace("Wrote anime file store to disk in {ElapsedTime}", stopwatch.Elapsed);
        }
        finally
        {
            WriteLock.Release();
        }
    }

    public async Task SaveAsync()
    {
        await WriteLock.WaitAsync();

        try
        {
            Logger?.LogTrace("Writing anime file store to disk");
            var stopwatch = Stopwatch.StartNew();

            await using var fs = File.Open(AppPaths.AnimeInfoFilePath, File.Exists(AppPaths.AnimeInfoFilePath) ? FileMode.Truncate : FileMode.Create);
            await JsonSerializer.SerializeAsync(fs, Anime, SerializerOptions);
            await fs.FlushAsync();

            Logger?.LogTrace("Wrote anime file store to disk in {ElapsedTime}", stopwatch.Elapsed);
        }
        finally
        {
            WriteLock.Release();
        }
    }
}
