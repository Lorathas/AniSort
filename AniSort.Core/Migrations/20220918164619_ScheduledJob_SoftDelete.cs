using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AniSort.Core.Migrations
{
    public partial class ScheduledJob_SoftDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "ScheduledJobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "ScheduledJobs");
        }
    }
}
