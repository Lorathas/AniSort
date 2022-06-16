using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AniSort.Core.Migrations
{
    public partial class Nullable_EpisodeFile_ReleaseGroupID : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EpisodeFiles_ReleaseGroups_GroupId",
                table: "EpisodeFiles");

            migrationBuilder.AlterColumn<int>(
                name: "GroupId",
                table: "EpisodeFiles",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_EpisodeFiles_ReleaseGroups_GroupId",
                table: "EpisodeFiles",
                column: "GroupId",
                principalTable: "ReleaseGroups",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EpisodeFiles_ReleaseGroups_GroupId",
                table: "EpisodeFiles");

            migrationBuilder.AlterColumn<int>(
                name: "GroupId",
                table: "EpisodeFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EpisodeFiles_ReleaseGroups_GroupId",
                table: "EpisodeFiles",
                column: "GroupId",
                principalTable: "ReleaseGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
