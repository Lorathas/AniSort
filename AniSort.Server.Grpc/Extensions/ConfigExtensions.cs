using AniSort.Core;
using AniSort.Server.Data.Settings;

namespace AniSort.Server.Extensions;

public static class ConfigExtensions
{
    public static SettingsReply ToReply(this Config config)
    {
        var reply = new SettingsReply
        {
            Copy = config.Copy,
            Debug = config.Debug,
            Destination = config.Destination.ToReply(),
            Verbose = config.Verbose,
            AniDb = config.AniDb.ToReply(),
            IncrementalCleanup = config.IncrementalCleanup,
            IgnoreLibraryFiles = config.IgnoreLibraryFiles
        };
        
        reply.Sources.AddRange(config.Sources);
        reply.LibraryPaths.AddRange(config.LibraryPaths);

        return reply;
    }

    public static Config ToModel(this SettingsReply reply)
    {
        return new Config
        {
            Copy = reply.Copy,
            Debug = reply.Debug,
            Destination = reply.Destination.ToModel(),
            Verbose = reply.Verbose,
            AniDb = reply.AniDb.ToModel(),
            IncrementalCleanup = reply.IncrementalCleanup,
            IgnoreLibraryFiles = reply.IgnoreLibraryFiles,
            LibraryPaths = reply.LibraryPaths.ToList(),
            Sources = reply.Sources.ToList()
        };
    }

    public static DestinationConfigReply ToReply(this DestinationConfig destination)
    {
        return new DestinationConfigReply
        {
            Format = destination.Format,
            Path = destination.Path,
            FragmentSeries = destination.FragmentSeries,
            MoviePath = destination.MoviePath,
            TvPath = destination.TvPath
        };
    }

    public static DestinationConfig ToModel(this DestinationConfigReply reply)
    {
        return new DestinationConfig
        {
            Format = reply.Format,
            Path = reply.Path,
            FragmentSeries = reply.FragmentSeries,
            MoviePath = reply.MoviePath,
            TvPath = reply.TvPath
        };
    }

    public static AniDbConfigReply ToReply(this AniDbConfig aniDb)
    {
        var reply = new AniDbConfigReply
        {
            Username = aniDb.Username,
            Password = aniDb.Password,
            FileSearchCooldownMinutes = aniDb.FileSearchCooldownMinutes
        };

        if (aniDb.MaxFileSearchRetries.HasValue)
        {
            reply.MaxFileSearchRetries = aniDb.MaxFileSearchRetries.Value;
        }

        return reply;
    }
    
    public static AniDbConfig ToModel(this AniDbConfigReply reply)
    {
        var config = new AniDbConfig
        {
            Username = reply.Username,
            Password = reply.Password,
            FileSearchCooldownMinutes = reply.FileSearchCooldownMinutes
        };

        if (reply.HasMaxFileSearchRetries)
        {
            reply.MaxFileSearchRetries = reply.MaxFileSearchRetries;
        }

        return config;
    }
}
