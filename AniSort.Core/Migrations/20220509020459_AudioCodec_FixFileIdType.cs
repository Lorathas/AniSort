using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AniSort.Core.Migrations
{
    public partial class AudioCodec_FixFileIdType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AudioCodecs_EpisodeFiles_FileId1",
                table: "AudioCodecs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AudioCodecs",
                table: "AudioCodecs");

            migrationBuilder.DropIndex(
                name: "IX_AudioCodecs_FileId1",
                table: "AudioCodecs");

            migrationBuilder.DropColumn(
                name: "FileId1",
                table: "AudioCodecs");

            migrationBuilder.AlterColumn<int>(
                name: "FileId",
                table: "AudioCodecs",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "AudioCodecs",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_AudioCodecs",
                table: "AudioCodecs",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_AudioCodecs_FileId",
                table: "AudioCodecs",
                column: "FileId");

            migrationBuilder.AddForeignKey(
                name: "FK_AudioCodecs_EpisodeFiles_FileId",
                table: "AudioCodecs",
                column: "FileId",
                principalTable: "EpisodeFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AudioCodecs_EpisodeFiles_FileId",
                table: "AudioCodecs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AudioCodecs",
                table: "AudioCodecs");

            migrationBuilder.DropIndex(
                name: "IX_AudioCodecs_FileId",
                table: "AudioCodecs");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "AudioCodecs");

            migrationBuilder.AlterColumn<int>(
                name: "FileId",
                table: "AudioCodecs",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "FileId1",
                table: "AudioCodecs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AudioCodecs",
                table: "AudioCodecs",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_AudioCodecs_FileId1",
                table: "AudioCodecs",
                column: "FileId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AudioCodecs_EpisodeFiles_FileId1",
                table: "AudioCodecs",
                column: "FileId1",
                principalTable: "EpisodeFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
