using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AniSort.Core.Data;
using AniSort.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AniSort.Core.MaintenanceTasks;

public class AddExistingDataToDatabase : IMaintenanceTask
{
    private readonly IServiceProvider serviceProvider;
    private readonly AnimeFileStore animeFileStore = new();
    private readonly ILogger<AddExistingDataToDatabase> logger;
    private readonly List<FileImportStatus> importedFiles = new();

    public AddExistingDataToDatabase(IServiceProvider serviceProvider, ILogger<AddExistingDataToDatabase> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task RunAsync()
    {
        await using var scopeProvider = serviceProvider.CreateAsyncScope();
        await using var context = scopeProvider.ServiceProvider.GetService<AniSortContext>();

        var totalStopwatch = Stopwatch.StartNew();

        var createdAnime = 0;
        if (animeFileStore is { Anime.Count: > 0 })
        {
            Debug.Assert(context != null, nameof(context) + " != null");

            var existingGroups = context.ReleaseGroups.Select(g => g.Id).Distinct().ToHashSet();

            var existingShows = context.Anime.Select(a => a.Id).Distinct().ToHashSet();

            var storeStopwatch = Stopwatch.StartNew();

            foreach (var anime in animeFileStore.Anime.Values)
            {
                if (existingShows.Contains(anime.Id))
                {
                    continue;
                }
                var newAnime = new Anime
                {
                    Id = anime.Id,
                    TotalEpisodes = anime.TotalEpisodes,
                    HighestEpisodeNumber = anime.HighestEpisodeNumber,
                    Year = anime.Year,
                    Type = anime.Type,
                    ChildrenAnime = anime.RelatedAnimeIdList.Select(a => new RelatedAnime { DestinationAnimeId = a.Id, Relation = a.RelationType }).ToList(),
                    RomajiName = anime.RomajiName,
                    KanjiName = anime.KanjiName,
                    EnglishName = anime.EnglishName,
                    OtherName = anime.OtherName,
                    Synonyms = anime.SynonymNames.Select(s => new Synonym { Value = s }).ToList(),
                    Episodes = anime.Episodes.Select(e => new Episode
                    {
                        Id = e.Id,
                        Number = e.Number,
                        EnglishName = e.EnglishName,
                        RomajiName = e.RomajiName,
                        KanjiName = e.KanjiName,
                        Rating = e.Rating,
                        VoteCount = e.VoteCount,
                        Files = e.Files.Select(f => new EpisodeFile
                        {
                            Id = f.Id,
                            GroupId = f.GroupId != 0 ? f.GroupId : ReleaseGroup.UnknownId,
                            OtherEpisodes = f.OtherEpisodes,
                            IsDeprecated = f.IsDeprecated,
                            State = f.State,
                            Ed2kHash = f.Ed2kHash,
                            Md5Hash = f.Md5Hash,
                            Sha1Hash = f.Sha1Hash,
                            Crc32Hash = f.Crc32Hash,
                            VideoColorDepth = f.VideoColorDepth,
                            Quality = f.Quality,
                            Source = f.Source,
                            AudioCodecs = f.AudioCodecs.Select(c => new AudioCodec { Codec = c.CodecName, Bitrate = c.BitrateKbps }).ToList(),
                            VideoCodec = f.VideoCodec.CodecName,
                            VideoBitrate = f.VideoCodec.BitrateKbps,
                            VideoWidth = f.VideoResolution.Width,
                            VideoHeight = f.VideoResolution.Height,
                            FileType = f.FileType,
                            DubLanguage = f.DubLanguage,
                            SubLanguage = f.SubLanguage,
                            LengthInSeconds = f.LengthInSeconds,
                            Description = f.Description,
                            AiredDate = f.AiredDate,
                            AniDbFilename = f.AniDbFilename
                        }).ToList()
                    }).ToList()
                };

                var categoriesAdded = new HashSet<string>();

                var existingCategories = context.Categories.Where(c => anime.Categories.Contains(c.Value));

                foreach (var category in existingCategories)
                {
                    if (categoriesAdded.Contains(category.Value))
                    {
                        continue;
                    }

                    newAnime.Categories.Add(new AnimeCategory { CategoryId = category.Id });

                    categoriesAdded.Add(category.Value);
                }

                foreach (var category in anime.Categories)
                {
                    if (categoriesAdded.Contains(category))
                    {
                        continue;
                    }

                    newAnime.Categories.Add(new AnimeCategory { Category = new Category { Value = category } });

                    categoriesAdded.Add(category);
                }

                var groups = anime.Episodes.SelectMany(e => e.Files).Select(f => (f.GroupId, f.GroupName, f.GroupShortName)).Distinct().ToList();

                foreach (var group in groups)
                {
                    if (existingGroups.Contains(group.GroupId))
                    {
                        continue;
                    }

                    if (group.GroupId == 0)
                    {
                        if (!existingGroups.Contains(ReleaseGroup.UnknownId))
                        {
                            context.ReleaseGroups.Add(new ReleaseGroup { Id = ReleaseGroup.UnknownId, Name = string.Empty, ShortName = string.Empty });
                        }
                    }
                    else
                    {
                        context.ReleaseGroups.Add(new ReleaseGroup { Id = group.GroupId, Name = group.GroupName, ShortName = group.GroupShortName });
                    }

                    existingGroups.Add(group.GroupId);
                }

                context.Anime.Add(newAnime);
                createdAnime++;
            }

            await context.SaveChangesAsync();

            storeStopwatch.Stop();

            if (animeFileStore.Anime.Count > 0 && createdAnime > 0)
            {
                logger.LogDebug("Created {CreatedAnime} of {TotalAnime} anime from file store in {ElapsedTime}", createdAnime, animeFileStore.Anime.Count, storeStopwatch.Elapsed);
            }
        }

        var createdFiles = 0;
        if (importedFiles is { Count: > 0 })
        {
            var importsStopwatch = Stopwatch.StartNew();

            var existingFiles = context.LocalFiles.Select(f => f.Path).Distinct().ToHashSet();

            foreach (var fileImportStatus in importedFiles)
            {
                if (existingFiles.Contains(fileImportStatus.FilePath))
                {
                    continue;
                }

                var localFile = new LocalFile
                {
                    Path = fileImportStatus.FilePath,
                    Ed2kHash = fileImportStatus.Hash,
                    Status = fileImportStatus.Status,
                    EpisodeFileId = (await context.EpisodeFiles.FirstOrDefaultAsync(f => f.Ed2kHash == fileImportStatus.Hash))?.Id
                };

                if (fileImportStatus.Hash != null)
                {
                    localFile.FileActions.Add(new FileAction
                    {
                        Type = FileActionType.Hash,
                        Success = true,
                        Info = !string.IsNullOrWhiteSpace(fileImportStatus.Message) ? $"Legacy Message: {fileImportStatus.Message}" : null
                    });
                }

                for (var idx = fileImportStatus.Status is ImportStatus.Imported or ImportStatus.ImportedMissingData ? 1 : 0; idx < fileImportStatus.Attempts; idx++)
                {
                    localFile.FileActions.Add(new FileAction
                    {
                        Type = FileActionType.Search,
                        Success = false,
                        Info = !string.IsNullOrWhiteSpace(fileImportStatus.Message) ? $"Legacy Message: {fileImportStatus.Message}" : null
                    });
                }

                if (fileImportStatus.Status is ImportStatus.Imported or ImportStatus.ImportedMissingData)
                {
                    localFile.FileActions.Add(new FileAction
                    {
                        Type = FileActionType.Move,
                        Success = true,
                        Info = !string.IsNullOrWhiteSpace(fileImportStatus.Message) ? $"Legacy Message: {fileImportStatus.Message}" : null
                    });
                }

                context.LocalFiles.Add(localFile);
                createdFiles++;
            }

            await context.SaveChangesAsync();

            importsStopwatch.Stop();
            totalStopwatch.Stop();

            if (importedFiles.Count > 0 && createdFiles > 0)
            {
                logger.LogDebug("Created {CreatedFile} of {TotalFiles} files from file store in {ElapsedTime}", createdFiles, importedFiles.Count, importsStopwatch.Elapsed);
            }
        }
        if ((importedFiles?.Count > 0 && createdFiles > 0) || (animeFileStore.Anime.Count > 0 && createdAnime > 0))
        {
            logger.LogDebug("Updated database with local files in {ElapsedTime}", totalStopwatch.Elapsed);
        }
    }

    /// <inheritdoc />
    public string Description => "";

    /// <inheritdoc />
    public string CommandName => "upgradetodb";
}
