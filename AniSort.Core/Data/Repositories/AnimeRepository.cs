using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AniDbSharp.Data;
using AniSort.Core.Extensions;
using AniSort.Core.Models;
using FFMpegCore.Enums;
using Microsoft.EntityFrameworkCore;
using FileInfo = AniSort.Core.Models.FileInfo;

namespace AniSort.Core.Data.Repositories;

public class AnimeRepository : RepositoryBase<Anime, int, AniSortContext>, IAnimeRepository
{

    /// <inheritdoc />
    public AnimeRepository(AniSortContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public Anime MergeSert(FileResult result)
    {
        if (result.FileInfo.AnimeId == null)
        {
            throw new ArgumentNullException(nameof(result.FileInfo.AnimeId));
        }

        if (result.FileInfo.EpisodeId == null)
        {
            throw new ArgumentNullException(nameof(result.FileInfo.EpisodeId));
        }

        if (result.FileInfo.GroupId == null)
        {
            throw new ArgumentNullException(nameof(result.FileInfo.GroupId));
        }

        var anime = Set
            .Include(a => a.Categories)
            .ThenInclude(c => c.Category)
            .Include(a => a.Episodes)
            .Include(a => a.Synonyms)
            .Include(a => a.ChildrenAnime)
            .FirstOrDefault(a => a.Id == result.FileInfo.AnimeId);
        
        if (anime == null)
        {
            anime = new Anime
            {
                Id = result.FileInfo.AnimeId.Value,
                TotalEpisodes = result.AnimeInfo.TotalEpisodes ?? 0,
                HighestEpisodeNumber = result.AnimeInfo.HighestEpisodeNumber ?? 0,
                Year = !string.IsNullOrWhiteSpace(result.AnimeInfo.Year) ? int.Parse(result.AnimeInfo.Year) : 0,
                Type = result.AnimeInfo.Type,
                RomajiName = result.AnimeInfo.RomajiName,
                KanjiName = result.AnimeInfo.KanjiName,
                EnglishName = result.AnimeInfo.EnglishName,
                OtherName = result.AnimeInfo.OtherName,
            };
            Add(anime);
        }
        else
        {
            anime.TotalEpisodes = result.AnimeInfo.TotalEpisodes ?? anime.TotalEpisodes;
            anime.HighestEpisodeNumber = result.AnimeInfo.HighestEpisodeNumber ?? anime.HighestEpisodeNumber;
            anime.Year = !string.IsNullOrWhiteSpace(result.AnimeInfo.Year) ? int.Parse(result.AnimeInfo.Year) : anime.Year;
            anime.Type = result.AnimeInfo.Type ?? anime.Type;
            anime.RomajiName = result.AnimeInfo.RomajiName ?? anime.RomajiName;
            anime.KanjiName = result.AnimeInfo.KanjiName ?? anime.KanjiName;
            anime.EnglishName = result.AnimeInfo.EnglishName ?? anime.EnglishName;
            anime.OtherName = result.AnimeInfo.OtherName ?? anime.OtherName;
        }

        #region Relations
        
        #region Related Anime
        
        if (!string.IsNullOrWhiteSpace(result.AnimeInfo.RelatedAnimeIdList) && !string.IsNullOrWhiteSpace(result.AnimeInfo.RelatedAnimeIdType))
        {
            var childrenAnimeIds = result.AnimeInfo.RelatedAnimeIdList.Split(',')
                .Select(int.Parse)
                .Distinct()
                .ToHashSet();

            var existingChildrenAnime = Context.Anime.Where(a => childrenAnimeIds.Contains(a.Id)).Select(a => a.Id).Distinct().ToHashSet();
            var animeChildrenAnime = anime.ChildrenAnime.Select(c => c.DestinationAnimeId).ToHashSet();

            foreach (var (animeId, relation) in childrenAnimeIds.Zip(result.AnimeInfo.RelatedAnimeIdType.Split(',')))
            {
                if (existingChildrenAnime.Contains(animeId) && !animeChildrenAnime.Contains(animeId))
                {
                    anime.ChildrenAnime.Add(new RelatedAnime { DestinationAnimeId = animeId, Relation = relation });
                }
            }
        }
        
        #endregion
        
        #region Categories

        if (!string.IsNullOrWhiteSpace(result.AnimeInfo.CategoryList))
        {
            var categories = result.AnimeInfo.CategoryList.Split(',').Distinct().ToHashSet();

            var existingCategories = Context.Categories.Where(c => categories.Contains(c.Value)).ToDictionary(c => c.Value);
            var animeCategories = anime.Categories.Select(c => c.Category.Value).Distinct().ToHashSet();

            foreach (var category in categories)
            {
                if (animeCategories.Contains(category))
                {
                    continue;
                }
                
                if (existingCategories.TryGetValue(category, out var existingCategory))
                {
                    anime.Categories.Add(new AnimeCategory
                    {
                        Category = existingCategory
                    });
                }
                else
                {
                    anime.Categories.Add(new AnimeCategory
                    {
                        Category = new Category
                        {
                            Value = category
                        }
                    });
                }
            }
        }
        
        #endregion
        
        #region Synonyms

        if (!string.IsNullOrWhiteSpace(result.AnimeInfo.SynonymList))
        {
            var synonyms = result.AnimeInfo.SynonymList.Split(',');

            var existingSynonyms = anime.Synonyms.Select(s => s.Value).Distinct().ToHashSet();

            foreach (var synonym in synonyms)
            {
                if (existingSynonyms.Contains(synonym))
                {
                    continue;
                }
                
                anime.Synonyms.Add(new Synonym { Value = synonym});
            }
        }
        
        #endregion
        
        #region Episode

        var episode = anime.Episodes.FirstOrDefault(e => e.Id == result.FileInfo.EpisodeId.Value);

        if (episode == default)
        {
            episode = new Episode
            {
                Id = result.FileInfo.EpisodeId.Value,
                Number = result.AnimeInfo.EpisodeNumber,
                EnglishName = result.AnimeInfo.EpisodeName,
                Rating = result.AnimeInfo.EpisodeRating,
                VoteCount = result.AnimeInfo.EpisodeVoteCount,
            };
            anime.Episodes.Add(episode);
        }
        else
        {
            episode.Number = result.AnimeInfo.EpisodeNumber;
            episode.EnglishName = result.AnimeInfo.EnglishName;
            episode.Rating = result.AnimeInfo.EpisodeRating;
            episode.VoteCount = result.AnimeInfo.EpisodeVoteCount;
        }
        
        #endregion
        
        #region Episode File

        var file = episode.Files.FirstOrDefault(f => f.Id == result.FileInfo.FileId);

        if (file == default)
        {
            file = new EpisodeFile
            {
                Id = result.FileInfo.FileId,
                OtherEpisodes = result.FileInfo.OtherEpisodes,
                IsDeprecated = result.FileInfo.IsDeprecated == 1,
                State = result.FileInfo.State ?? 0,
                Ed2kHash = result.FileInfo.Ed2kHash,
                Md5Hash = result.FileInfo.Md5Hash,
                Sha1Hash = result.FileInfo.Sha1Hash,
                Crc32Hash = result.FileInfo.Crc32Hash,
                VideoColorDepth = result.FileInfo.VideoColorDepth,
                Quality = result.FileInfo.Quality,
                Source = result.FileInfo.Source,
                FileType = result.FileInfo.FileType,
                DubLanguage = result.FileInfo.DubLanguage,
                SubLanguage = result.FileInfo.SubLanguage,
                LengthInSeconds = result.FileInfo.LengthInSeconds,
                Description = result.FileInfo.Description,
                AiredDate = result.FileInfo.AiredDate.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(result.FileInfo.AiredDate.Value).UtcDateTime : default,
                AniDbFilename = result.FileInfo.AniDbFilename,
                Resolution = result.FileInfo.VideoResolution.ParseVideoResolution()
            };
        }
        else
        {
            file.OtherEpisodes = result.FileInfo.OtherEpisodes ?? file.OtherEpisodes;
            file.IsDeprecated = result.FileInfo.IsDeprecated == 1;
            file.State = result.FileInfo.State ??  file.State;
            file.Ed2kHash = result.FileInfo.Ed2kHash ?? file.Ed2kHash;
            file.Md5Hash = result.FileInfo.Md5Hash ?? file.Md5Hash;
            file.Sha1Hash = result.FileInfo.Sha1Hash ?? file.Sha1Hash;
            file.Crc32Hash = result.FileInfo.Crc32Hash ?? file.Crc32Hash;
            file.VideoColorDepth = result.FileInfo.VideoColorDepth ?? file.VideoColorDepth;
            file.Quality = result.FileInfo.Quality ?? file.Quality;
            file.Source = result.FileInfo.Source ?? file.Source;
            file.FileType = result.FileInfo.FileType ?? file.FileType;
            file.DubLanguage = result.FileInfo.DubLanguage ?? file.DubLanguage;
            file.SubLanguage = result.FileInfo.SubLanguage ?? file.SubLanguage;
            file.LengthInSeconds = result.FileInfo.LengthInSeconds ?? file.LengthInSeconds;
            file.Description = result.FileInfo.Description ?? file.Description;
            file.AiredDate = result.FileInfo.AiredDate.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(result.FileInfo.AiredDate.Value).UtcDateTime : file.AiredDate;
            file.AniDbFilename = result.FileInfo.AniDbFilename ?? file.AniDbFilename;
            file.Resolution = result.FileInfo.VideoResolution.ParseVideoResolution() ?? file.Resolution;
        }
            
        #region Audio Codecs

        if (!string.IsNullOrWhiteSpace(result.FileInfo.AudioCodecList) && !string.IsNullOrWhiteSpace(result.FileInfo.AudioBitrateList))
        {
            var seenCodecs = new HashSet<(string Codec, int Bitrate)>();
                
            foreach (var (codec, bitrate) in result.FileInfo.AudioCodecList.Split(',')
                         .Zip(result.FileInfo.AudioBitrateList.Split(',').Select(int.Parse))
                         .Select(pair => new CodecInfo(pair.First, pair.Second)))
            {
                if (!file.AudioCodecs.Any(c => c.Codec == codec && c.Bitrate == bitrate))
                {
                    file.AudioCodecs.Add(new AudioCodec { Codec = codec, Bitrate = bitrate });
                    seenCodecs.Add((codec, bitrate));
                }
            }

            file.AudioCodecs
                .Where(audioCodec => !seenCodecs.Contains((audioCodec.Codec, audioCodec.Bitrate)))
                .ToList()
                .ForEach(c => file.AudioCodecs.Remove(c));
        }
            
        #endregion
            
        #region Release Group

        if (file.Group == null)
        {
            var group = Context.ReleaseGroups.FirstOrDefault(g => g.Id == result.FileInfo.GroupId);

            if (group == default)
            {
                file.Group = new ReleaseGroup
                {
                    Id = result.FileInfo.GroupId.Value,
                    Name = result.AnimeInfo.GroupName,
                    ShortName = result.AnimeInfo.GroupShortName
                };
            }
            else
            {
                file.Group = group;
            }
        }
            
        #endregion

        #endregion

        #endregion

        return anime;
    }

    /// <inheritdoc />
    public async Task<Anime> MergeSertAsync(FileResult result)
    {
        if (result.FileInfo.AnimeId == null)
        {
            throw new ArgumentNullException(nameof(result.FileInfo.AnimeId));
        }

        if (result.FileInfo.EpisodeId == null)
        {
            throw new ArgumentNullException(nameof(result.FileInfo.EpisodeId));
        }

        if (result.FileInfo.GroupId == null)
        {
            throw new ArgumentNullException(nameof(result.FileInfo.GroupId));
        }

        if (result.FileInfo.FileId == default)
        {
            throw new ArgumentNullException(nameof(result.FileInfo.FileId));
        }

        var anime = await Set
            .Include(a => a.Categories)
            .ThenInclude(c => c.Category)
            .Include(a => a.Episodes)
            .Include(a => a.Synonyms)
            .Include(a => a.ChildrenAnime)
            .FirstOrDefaultAsync(a => a.Id == result.FileInfo.AnimeId);
        
        if (anime == null)
        {
            anime = new Anime
            {
                Id = result.FileInfo.AnimeId.Value,
                TotalEpisodes = result.AnimeInfo.TotalEpisodes ?? 0,
                HighestEpisodeNumber = result.AnimeInfo.HighestEpisodeNumber ?? 0,
                Year = !string.IsNullOrWhiteSpace(result.AnimeInfo.Year) ? int.Parse(result.AnimeInfo.Year) : 0,
                Type = result.AnimeInfo.Type,
                RomajiName = result.AnimeInfo.RomajiName,
                KanjiName = result.AnimeInfo.KanjiName,
                EnglishName = result.AnimeInfo.EnglishName,
                OtherName = result.AnimeInfo.OtherName,
            };
            await AddAsync(anime);
        }
        else
        {
            anime.TotalEpisodes = result.AnimeInfo.TotalEpisodes ?? anime.TotalEpisodes;
            anime.HighestEpisodeNumber = result.AnimeInfo.HighestEpisodeNumber ?? anime.HighestEpisodeNumber;
            anime.Year = !string.IsNullOrWhiteSpace(result.AnimeInfo.Year) ? int.Parse(result.AnimeInfo.Year) : anime.Year;
            anime.Type = result.AnimeInfo.Type;
            anime.RomajiName = result.AnimeInfo.RomajiName ?? anime.RomajiName;
            anime.KanjiName = result.AnimeInfo.KanjiName ?? anime.KanjiName;
            anime.EnglishName = result.AnimeInfo.EnglishName ?? anime.EnglishName;
            anime.OtherName = result.AnimeInfo.OtherName ?? anime.OtherName;
        }

        #region Relations
        
        #region Related Anime
        
        if (!string.IsNullOrWhiteSpace(result.AnimeInfo.RelatedAnimeIdList) && !string.IsNullOrWhiteSpace(result.AnimeInfo.RelatedAnimeIdType))
        {
            var childrenAnimeIds = result.AnimeInfo.RelatedAnimeIdList.Split(',')
                .Select(int.Parse)
                .Distinct()
                .ToHashSet();

            var existingChildrenAnime = Context.Anime.Where(a => childrenAnimeIds.Contains(a.Id)).Select(a => a.Id).Distinct().ToHashSet();
            var animeChildrenAnime = anime.ChildrenAnime.Select(c => c.DestinationAnimeId).ToHashSet();

            foreach (var (animeId, relation) in childrenAnimeIds.Zip(result.AnimeInfo.RelatedAnimeIdType.Split(',')))
            {
                if (existingChildrenAnime.Contains(animeId) && !animeChildrenAnime.Contains(animeId))
                {
                    anime.ChildrenAnime.Add(new RelatedAnime { DestinationAnimeId = animeId, Relation = relation });
                }
            }
        }
        
        #endregion
        
        #region Categories

        if (!string.IsNullOrWhiteSpace(result.AnimeInfo.CategoryList))
        {
            var categories = result.AnimeInfo.CategoryList.Split(',').Distinct().ToHashSet();

            var existingCategories = Context.Categories.Where(c => categories.Contains(c.Value)).ToDictionary(c => c.Value);
            var animeCategories = anime.Categories.Select(c => c.Category.Value).Distinct().ToHashSet();

            foreach (var category in categories)
            {
                if (animeCategories.Contains(category))
                {
                    continue;
                }
                
                if (existingCategories.TryGetValue(category, out var existingCategory))
                {
                    anime.Categories.Add(new AnimeCategory
                    {
                        Category = existingCategory
                    });
                }
                else
                {
                    anime.Categories.Add(new AnimeCategory
                    {
                        Category = new Category
                        {
                            Value = category
                        }
                    });
                }
            }
        }
        
        #endregion
        
        #region Synonyms

        if (!string.IsNullOrWhiteSpace(result.AnimeInfo.SynonymList))
        {
            var synonyms = result.AnimeInfo.SynonymList.Split(',');

            var existingSynonyms = anime.Synonyms.Select(s => s.Value).Distinct().ToHashSet();

            foreach (var synonym in synonyms)
            {
                if (existingSynonyms.Contains(synonym))
                {
                    continue;
                }
                
                anime.Synonyms.Add(new Synonym { Value = synonym});
            }
        }
        
        #endregion
        
        #region Episode

        // ReSharper disable once PossibleInvalidOperationException
        var episode = anime.Episodes.FirstOrDefault(e => e.Id == result.FileInfo.EpisodeId.Value);

        if (episode == default)
        {
            episode = new Episode
            {
                Id = result.FileInfo.EpisodeId.Value,
                Number = result.AnimeInfo.EpisodeNumber,
                EnglishName = result.AnimeInfo.EpisodeName,
                Rating = result.AnimeInfo.EpisodeRating,
                VoteCount = result.AnimeInfo.EpisodeVoteCount,
            };
            anime.Episodes.Add(episode);
        }
        else
        {
            episode.Number = result.AnimeInfo.EpisodeNumber;
            episode.EnglishName = result.AnimeInfo.EnglishName;
            episode.Rating = result.AnimeInfo.EpisodeRating;
            episode.VoteCount = result.AnimeInfo.EpisodeVoteCount;
        }
        
        #endregion
        
        #region Episode File

        var file = episode.Files.FirstOrDefault(f => f.Id == result.FileInfo.FileId);

        if (file == default)
        {
            file = new EpisodeFile
            {
                Id = result.FileInfo.FileId,
                OtherEpisodes = result.FileInfo.OtherEpisodes,
                IsDeprecated = result.FileInfo.IsDeprecated == 1,
                State = result.FileInfo.State ?? 0,
                Ed2kHash = result.FileInfo.Ed2kHash,
                Md5Hash = result.FileInfo.Md5Hash,
                Sha1Hash = result.FileInfo.Sha1Hash,
                Crc32Hash = result.FileInfo.Crc32Hash,
                VideoColorDepth = result.FileInfo.VideoColorDepth,
                Quality = result.FileInfo.Quality,
                Source = result.FileInfo.Source,
                FileType = result.FileInfo.FileType,
                DubLanguage = result.FileInfo.DubLanguage,
                SubLanguage = result.FileInfo.SubLanguage,
                LengthInSeconds = result.FileInfo.LengthInSeconds,
                Description = result.FileInfo.Description,
                AiredDate = result.FileInfo.AiredDate.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(result.FileInfo.AiredDate.Value).UtcDateTime : default,
                AniDbFilename = result.FileInfo.AniDbFilename,
                Resolution = result.FileInfo.VideoResolution.ParseVideoResolution()
            };
        }
        else
        {
            file.OtherEpisodes = result.FileInfo.OtherEpisodes ?? file.OtherEpisodes;
            file.IsDeprecated = result.FileInfo.IsDeprecated == 1;
            file.State = result.FileInfo.State ??  file.State;
            file.Ed2kHash = result.FileInfo.Ed2kHash ?? file.Ed2kHash;
            file.Md5Hash = result.FileInfo.Md5Hash ?? file.Md5Hash;
            file.Sha1Hash = result.FileInfo.Sha1Hash ?? file.Sha1Hash;
            file.Crc32Hash = result.FileInfo.Crc32Hash ?? file.Crc32Hash;
            file.VideoColorDepth = result.FileInfo.VideoColorDepth ?? file.VideoColorDepth;
            file.Quality = result.FileInfo.Quality ?? file.Quality;
            file.Source = result.FileInfo.Source ?? file.Source;
            file.FileType = result.FileInfo.FileType ?? file.FileType;
            file.DubLanguage = result.FileInfo.DubLanguage ?? file.DubLanguage;
            file.SubLanguage = result.FileInfo.SubLanguage ?? file.SubLanguage;
            file.LengthInSeconds = result.FileInfo.LengthInSeconds ?? file.LengthInSeconds;
            file.Description = result.FileInfo.Description ?? file.Description;
            file.AiredDate = result.FileInfo.AiredDate.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(result.FileInfo.AiredDate.Value).UtcDateTime : file.AiredDate;
            file.AniDbFilename = result.FileInfo.AniDbFilename ?? file.AniDbFilename;
            file.Resolution = result.FileInfo.VideoResolution.ParseVideoResolution() ?? file.Resolution;
        }
            
        #region Audio Codecs

        if (!string.IsNullOrWhiteSpace(result.FileInfo.AudioCodecList) && !string.IsNullOrWhiteSpace(result.FileInfo.AudioBitrateList))
        {
            var seenCodecs = new HashSet<(string Codec, int Bitrate)>();
                
            foreach (var (codec, bitrate) in result.FileInfo.AudioCodecList.Split(',')
                         .Zip(result.FileInfo.AudioBitrateList.Split(',').Select(int.Parse))
                         .Select(pair => new CodecInfo(pair.First, pair.Second)))
            {
                if (!file.AudioCodecs.Any(c => c.Codec == codec && c.Bitrate == bitrate))
                {
                    file.AudioCodecs.Add(new AudioCodec { Codec = codec, Bitrate = bitrate });
                    seenCodecs.Add((codec, bitrate));
                }
            }

            file.AudioCodecs
                .Where(audioCodec => !seenCodecs.Contains((audioCodec.Codec, audioCodec.Bitrate)))
                .ToList()
                .ForEach(c => file.AudioCodecs.Remove(c));
        }
            
        #endregion
            
        #region Release Group

        if (file.Group == null)
        {
            var group = Context.ReleaseGroups.FirstOrDefault(g => g.Id == result.FileInfo.GroupId);

            if (group == default)
            {
                file.Group = new ReleaseGroup
                {
                    Id = result.FileInfo.GroupId.Value,
                    Name = result.AnimeInfo.GroupName,
                    ShortName = result.AnimeInfo.GroupShortName
                };
            }
            else
            {
                file.Group = group;
            }
        }
            
        #endregion

        #endregion

        #endregion

        return anime;
    }

    /// <inheritdoc />
    public Anime MergeSert(FileResult result, LocalFile localFile)
    {
        var anime = MergeSert(result);

        var file = anime.Episodes.SelectMany(e => e.Files).FirstOrDefault(f => f.Id == result.FileInfo.FileId);

        if (file != null && !file.LocalFiles.Contains(localFile))
        {
            file.LocalFiles.Add(localFile);
        }

        return anime;
    }

    /// <inheritdoc />
    public async Task<Anime> MergeSertAsync(FileResult result, LocalFile localFile)
    {
        var anime = await MergeSertAsync(result);

        var file = anime.Episodes.SelectMany(e => e.Files).FirstOrDefault(f => f.Id == result.FileInfo.FileId);

        if (file != null && !file.LocalFiles.Contains(localFile))
        {
            file.LocalFiles.Add(localFile);
        }

        return anime;
    }
}
