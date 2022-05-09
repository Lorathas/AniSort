using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AniSort.Core.Migrations
{
    public partial class LocalFile_Length : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FileLength",
                table: "LocalFile",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "VideoBitrate",
                table: "EpisodeFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileLength",
                table: "LocalFile");

            migrationBuilder.DropColumn(
                name: "VideoBitrate",
                table: "EpisodeFiles");
        }
    }
}
