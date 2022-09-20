using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AniSort.Core.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Anime",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TotalEpisodes = table.Column<int>(type: "integer", nullable: false),
                    HighestEpisodeNumber = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    RomajiName = table.Column<string>(type: "text", nullable: false),
                    KanjiName = table.Column<string>(type: "text", nullable: false),
                    EnglishName = table.Column<string>(type: "text", nullable: false),
                    OtherName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anime", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ShortName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ScheduleType = table.Column<int>(type: "integer", nullable: false),
                    ScheduleOptions = table.Column<byte[]>(type: "bytea", nullable: false),
                    Options = table.Column<byte[]>(type: "bytea", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Episodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnimeId = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    EnglishName = table.Column<string>(type: "text", nullable: false),
                    RomajiName = table.Column<string>(type: "text", nullable: false),
                    KanjiName = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: true),
                    VoteCount = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Episodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Episodes_Anime_AnimeId",
                        column: x => x.AnimeId,
                        principalTable: "Anime",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RelatedAnime",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceAnimeId = table.Column<int>(type: "integer", nullable: false),
                    DestinationAnimeId = table.Column<int>(type: "integer", nullable: false),
                    Relation = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatedAnime", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RelatedAnime_Anime_DestinationAnimeId",
                        column: x => x.DestinationAnimeId,
                        principalTable: "Anime",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RelatedAnime_Anime_SourceAnimeId",
                        column: x => x.SourceAnimeId,
                        principalTable: "Anime",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Synonyms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    AnimeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Synonyms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Synonyms_Anime_AnimeId",
                        column: x => x.AnimeId,
                        principalTable: "Anime",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnimeCategories",
                columns: table => new
                {
                    AnimeId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimeCategories", x => new { x.AnimeId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_AnimeCategories_Anime_AnimeId",
                        column: x => x.AnimeId,
                        principalTable: "Anime",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnimeCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EpisodeFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EpisodeId = table.Column<int>(type: "integer", nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: true),
                    OtherEpisodes = table.Column<string>(type: "text", nullable: false),
                    IsDeprecated = table.Column<bool>(type: "boolean", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    Ed2kHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    Md5Hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    Sha1Hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    Crc32Hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    VideoColorDepth = table.Column<string>(type: "text", nullable: false),
                    Quality = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    VideoCodec = table.Column<string>(type: "text", nullable: false),
                    VideoBitrate = table.Column<int>(type: "integer", nullable: false),
                    VideoWidth = table.Column<int>(type: "integer", nullable: false),
                    VideoHeight = table.Column<int>(type: "integer", nullable: false),
                    FileType = table.Column<string>(type: "text", nullable: false),
                    DubLanguage = table.Column<string>(type: "text", nullable: false),
                    SubLanguage = table.Column<string>(type: "text", nullable: false),
                    LengthInSeconds = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    AiredDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AniDbFilename = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpisodeFiles_Episodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "Episodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EpisodeFiles_ReleaseGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "ReleaseGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AudioCodecs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<int>(type: "integer", nullable: false),
                    Codec = table.Column<string>(type: "text", nullable: false),
                    Bitrate = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioCodecs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AudioCodecs_EpisodeFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "EpisodeFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocalFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EpisodeFileId = table.Column<int>(type: "integer", nullable: true),
                    Path = table.Column<string>(type: "text", nullable: false),
                    Ed2kHash = table.Column<byte[]>(type: "bytea", nullable: true),
                    FileLength = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalFiles_EpisodeFiles_EpisodeFileId",
                        column: x => x.EpisodeFileId,
                        principalTable: "EpisodeFiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FileActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Info = table.Column<string>(type: "text", nullable: false),
                    Exception = table.Column<string>(type: "text", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileActions_LocalFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "LocalFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    QueuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Options = table.Column<byte[]>(type: "bytea", nullable: false),
                    LocalFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledJobId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jobs_LocalFiles_LocalFileId",
                        column: x => x.LocalFileId,
                        principalTable: "LocalFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Jobs_ScheduledJobs_ScheduledJobId",
                        column: x => x.ScheduledJobId,
                        principalTable: "ScheduledJobs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Params = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobLogs_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentProgress = table.Column<long>(type: "bigint", nullable: false),
                    TotalProgress = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobSteps_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobStepLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StepId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Params = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobStepLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobStepLogs_JobSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "JobSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnimeCategories_CategoryId",
                table: "AnimeCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AudioCodecs_FileId",
                table: "AudioCodecs",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Value",
                table: "Categories",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeFiles_EpisodeId",
                table: "EpisodeFiles",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeFiles_GroupId",
                table: "EpisodeFiles",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_AnimeId",
                table: "Episodes",
                column: "AnimeId");

            migrationBuilder.CreateIndex(
                name: "IX_FileActions_FileId",
                table: "FileActions",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_JobLogs_JobId",
                table: "JobLogs",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_LocalFileId",
                table: "Jobs",
                column: "LocalFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ScheduledJobId",
                table: "Jobs",
                column: "ScheduledJobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobStepLogs_StepId",
                table: "JobStepLogs",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSteps_JobId",
                table: "JobSteps",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalFiles_EpisodeFileId",
                table: "LocalFiles",
                column: "EpisodeFileId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedAnime_DestinationAnimeId",
                table: "RelatedAnime",
                column: "DestinationAnimeId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedAnime_SourceAnimeId",
                table: "RelatedAnime",
                column: "SourceAnimeId");

            migrationBuilder.CreateIndex(
                name: "IX_Synonyms_AnimeId",
                table: "Synonyms",
                column: "AnimeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnimeCategories");

            migrationBuilder.DropTable(
                name: "AudioCodecs");

            migrationBuilder.DropTable(
                name: "FileActions");

            migrationBuilder.DropTable(
                name: "JobLogs");

            migrationBuilder.DropTable(
                name: "JobStepLogs");

            migrationBuilder.DropTable(
                name: "RelatedAnime");

            migrationBuilder.DropTable(
                name: "Synonyms");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "JobSteps");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "LocalFiles");

            migrationBuilder.DropTable(
                name: "ScheduledJobs");

            migrationBuilder.DropTable(
                name: "EpisodeFiles");

            migrationBuilder.DropTable(
                name: "Episodes");

            migrationBuilder.DropTable(
                name: "ReleaseGroups");

            migrationBuilder.DropTable(
                name: "Anime");
        }
    }
}
