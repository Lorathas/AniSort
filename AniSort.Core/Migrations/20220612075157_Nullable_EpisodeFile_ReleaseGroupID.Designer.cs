﻿// <auto-generated />
using System;
using AniSort.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AniSort.Core.Migrations
{
    [DbContext(typeof(AniSortContext))]
    [Migration("20220612075157_Nullable_EpisodeFile_ReleaseGroupID")]
    partial class Nullable_EpisodeFile_ReleaseGroupID
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.5");

            modelBuilder.Entity("AniSort.Core.Data.Anime", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("EnglishName")
                        .HasColumnType("TEXT");

                    b.Property<int>("HighestEpisodeNumber")
                        .HasColumnType("INTEGER");

                    b.Property<string>("KanjiName")
                        .HasColumnType("TEXT");

                    b.Property<string>("OtherName")
                        .HasColumnType("TEXT");

                    b.Property<string>("RomajiName")
                        .HasColumnType("TEXT");

                    b.Property<int>("TotalEpisodes")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Type")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<int>("Year")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Anime");
                });

            modelBuilder.Entity("AniSort.Core.Data.AnimeCategory", b =>
                {
                    b.Property<int>("AnimeId")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("CategoryId")
                        .HasColumnType("TEXT");

                    b.HasKey("AnimeId", "CategoryId");

                    b.HasIndex("CategoryId");

                    b.ToTable("AnimeCategories");
                });

            modelBuilder.Entity("AniSort.Core.Data.AudioCodec", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("Bitrate")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Codec")
                        .HasColumnType("TEXT");

                    b.Property<int>("FileId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("FileId");

                    b.ToTable("AudioCodecs");
                });

            modelBuilder.Entity("AniSort.Core.Data.Category", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Value")
                        .IsUnique();

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("AniSort.Core.Data.Episode", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AnimeId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("EnglishName")
                        .HasColumnType("TEXT");

                    b.Property<string>("KanjiName")
                        .HasColumnType("TEXT");

                    b.Property<string>("Number")
                        .HasColumnType("TEXT");

                    b.Property<int?>("Rating")
                        .HasColumnType("INTEGER");

                    b.Property<string>("RomajiName")
                        .HasColumnType("TEXT");

                    b.Property<int?>("VoteCount")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AnimeId");

                    b.ToTable("Episodes");
                });

            modelBuilder.Entity("AniSort.Core.Data.EpisodeFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("AiredDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("AniDbFilename")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Crc32Hash")
                        .HasColumnType("BLOB");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<string>("DubLanguage")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Ed2kHash")
                        .HasColumnType("BLOB");

                    b.Property<int>("EpisodeId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("FileType")
                        .HasColumnType("TEXT");

                    b.Property<int?>("GroupId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsDeprecated")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("LengthInSeconds")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Md5Hash")
                        .HasColumnType("BLOB");

                    b.Property<string>("OtherEpisodes")
                        .HasColumnType("TEXT");

                    b.Property<string>("Quality")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Sha1Hash")
                        .HasColumnType("BLOB");

                    b.Property<string>("Source")
                        .HasColumnType("TEXT");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("SubLanguage")
                        .HasColumnType("TEXT");

                    b.Property<int>("VideoBitrate")
                        .HasColumnType("INTEGER");

                    b.Property<string>("VideoCodec")
                        .HasColumnType("TEXT");

                    b.Property<string>("VideoColorDepth")
                        .HasColumnType("TEXT");

                    b.Property<int>("VideoHeight")
                        .HasColumnType("INTEGER");

                    b.Property<int>("VideoWidth")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("EpisodeId");

                    b.HasIndex("GroupId");

                    b.ToTable("EpisodeFiles");
                });

            modelBuilder.Entity("AniSort.Core.Data.FileAction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Exception")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("FileId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Info")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Success")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("FileId");

                    b.ToTable("FileActions");
                });

            modelBuilder.Entity("AniSort.Core.Data.LocalFile", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("Ed2kHash")
                        .HasColumnType("BLOB");

                    b.Property<int?>("EpisodeFileId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("FileLength")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Path")
                        .HasColumnType("TEXT");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("EpisodeFileId");

                    b.ToTable("LocalFiles");
                });

            modelBuilder.Entity("AniSort.Core.Data.RelatedAnime", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("DestinationAnimeId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Relation")
                        .HasColumnType("TEXT");

                    b.Property<int>("SourceAnimeId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("DestinationAnimeId");

                    b.HasIndex("SourceAnimeId");

                    b.ToTable("RelatedAnime");
                });

            modelBuilder.Entity("AniSort.Core.Data.ReleaseGroup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("ShortName")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("ReleaseGroups");
                });

            modelBuilder.Entity("AniSort.Core.Data.Synonym", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("AnimeId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("AnimeId");

                    b.ToTable("Synonyms");
                });

            modelBuilder.Entity("AniSort.Core.Data.AnimeCategory", b =>
                {
                    b.HasOne("AniSort.Core.Data.Anime", "Anime")
                        .WithMany("Categories")
                        .HasForeignKey("AnimeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("AniSort.Core.Data.Category", "Category")
                        .WithMany("Anime")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Anime");

                    b.Navigation("Category");
                });

            modelBuilder.Entity("AniSort.Core.Data.AudioCodec", b =>
                {
                    b.HasOne("AniSort.Core.Data.EpisodeFile", "File")
                        .WithMany("AudioCodecs")
                        .HasForeignKey("FileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("File");
                });

            modelBuilder.Entity("AniSort.Core.Data.Episode", b =>
                {
                    b.HasOne("AniSort.Core.Data.Anime", "Anime")
                        .WithMany("Episodes")
                        .HasForeignKey("AnimeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Anime");
                });

            modelBuilder.Entity("AniSort.Core.Data.EpisodeFile", b =>
                {
                    b.HasOne("AniSort.Core.Data.Episode", "Episode")
                        .WithMany("Files")
                        .HasForeignKey("EpisodeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("AniSort.Core.Data.ReleaseGroup", "Group")
                        .WithMany("Files")
                        .HasForeignKey("GroupId");

                    b.Navigation("Episode");

                    b.Navigation("Group");
                });

            modelBuilder.Entity("AniSort.Core.Data.FileAction", b =>
                {
                    b.HasOne("AniSort.Core.Data.LocalFile", "File")
                        .WithMany("FileActions")
                        .HasForeignKey("FileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("File");
                });

            modelBuilder.Entity("AniSort.Core.Data.LocalFile", b =>
                {
                    b.HasOne("AniSort.Core.Data.EpisodeFile", "EpisodeFile")
                        .WithMany("LocalFiles")
                        .HasForeignKey("EpisodeFileId");

                    b.Navigation("EpisodeFile");
                });

            modelBuilder.Entity("AniSort.Core.Data.RelatedAnime", b =>
                {
                    b.HasOne("AniSort.Core.Data.Anime", "DestinationAnime")
                        .WithMany("ParentAnime")
                        .HasForeignKey("DestinationAnimeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("AniSort.Core.Data.Anime", "SourceAnime")
                        .WithMany("ChildrenAnime")
                        .HasForeignKey("SourceAnimeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DestinationAnime");

                    b.Navigation("SourceAnime");
                });

            modelBuilder.Entity("AniSort.Core.Data.Synonym", b =>
                {
                    b.HasOne("AniSort.Core.Data.Anime", "Anime")
                        .WithMany("Synonyms")
                        .HasForeignKey("AnimeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Anime");
                });

            modelBuilder.Entity("AniSort.Core.Data.Anime", b =>
                {
                    b.Navigation("Categories");

                    b.Navigation("ChildrenAnime");

                    b.Navigation("Episodes");

                    b.Navigation("ParentAnime");

                    b.Navigation("Synonyms");
                });

            modelBuilder.Entity("AniSort.Core.Data.Category", b =>
                {
                    b.Navigation("Anime");
                });

            modelBuilder.Entity("AniSort.Core.Data.Episode", b =>
                {
                    b.Navigation("Files");
                });

            modelBuilder.Entity("AniSort.Core.Data.EpisodeFile", b =>
                {
                    b.Navigation("AudioCodecs");

                    b.Navigation("LocalFiles");
                });

            modelBuilder.Entity("AniSort.Core.Data.LocalFile", b =>
                {
                    b.Navigation("FileActions");
                });

            modelBuilder.Entity("AniSort.Core.Data.ReleaseGroup", b =>
                {
                    b.Navigation("Files");
                });
#pragma warning restore 612, 618
        }
    }
}
