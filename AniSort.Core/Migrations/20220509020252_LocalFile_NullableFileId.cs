using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AniSort.Core.Migrations
{
    public partial class LocalFile_NullableFileId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileActions_LocalFile_FileId",
                table: "FileActions");

            migrationBuilder.DropForeignKey(
                name: "FK_LocalFile_EpisodeFiles_EpisodeFileId",
                table: "LocalFile");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LocalFile",
                table: "LocalFile");

            migrationBuilder.RenameTable(
                name: "LocalFile",
                newName: "LocalFiles");

            migrationBuilder.RenameIndex(
                name: "IX_LocalFile_EpisodeFileId",
                table: "LocalFiles",
                newName: "IX_LocalFiles_EpisodeFileId");

            migrationBuilder.AlterColumn<int>(
                name: "EpisodeFileId",
                table: "LocalFiles",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "LocalFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocalFiles",
                table: "LocalFiles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FileActions_LocalFiles_FileId",
                table: "FileActions",
                column: "FileId",
                principalTable: "LocalFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocalFiles_EpisodeFiles_EpisodeFileId",
                table: "LocalFiles",
                column: "EpisodeFileId",
                principalTable: "EpisodeFiles",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileActions_LocalFiles_FileId",
                table: "FileActions");

            migrationBuilder.DropForeignKey(
                name: "FK_LocalFiles_EpisodeFiles_EpisodeFileId",
                table: "LocalFiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LocalFiles",
                table: "LocalFiles");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "LocalFiles");

            migrationBuilder.RenameTable(
                name: "LocalFiles",
                newName: "LocalFile");

            migrationBuilder.RenameIndex(
                name: "IX_LocalFiles_EpisodeFileId",
                table: "LocalFile",
                newName: "IX_LocalFile_EpisodeFileId");

            migrationBuilder.AlterColumn<int>(
                name: "EpisodeFileId",
                table: "LocalFile",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LocalFile",
                table: "LocalFile",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FileActions_LocalFile_FileId",
                table: "FileActions",
                column: "FileId",
                principalTable: "LocalFile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LocalFile_EpisodeFiles_EpisodeFileId",
                table: "LocalFile",
                column: "EpisodeFileId",
                principalTable: "EpisodeFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
