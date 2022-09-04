using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AniSort.Core.Migrations
{
    public partial class ServerAndJobs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledJob",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduleType = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduleOptions = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Options = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledJob", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Job",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    QueuedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Options = table.Column<byte[]>(type: "BLOB", nullable: true),
                    LocalFileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScheduledJobId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Job_LocalFiles_LocalFileId",
                        column: x => x.LocalFileId,
                        principalTable: "LocalFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Job_ScheduledJob_ScheduledJobId",
                        column: x => x.ScheduledJobId,
                        principalTable: "ScheduledJob",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    Params = table.Column<byte[]>(type: "BLOB", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobLog_Job_JobId",
                        column: x => x.JobId,
                        principalTable: "Job",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobStep",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PercentComplete = table.Column<double>(type: "REAL", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobStep", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobStep_Job_JobId",
                        column: x => x.JobId,
                        principalTable: "Job",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StepLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StepId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    Params = table.Column<byte[]>(type: "BLOB", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StepLog_JobStep_StepId",
                        column: x => x.StepId,
                        principalTable: "JobStep",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Job_LocalFileId",
                table: "Job",
                column: "LocalFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Job_ScheduledJobId",
                table: "Job",
                column: "ScheduledJobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobLog_JobId",
                table: "JobLog",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobStep_JobId",
                table: "JobStep",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_StepLog_StepId",
                table: "StepLog",
                column: "StepId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobLog");

            migrationBuilder.DropTable(
                name: "StepLog");

            migrationBuilder.DropTable(
                name: "JobStep");

            migrationBuilder.DropTable(
                name: "Job");

            migrationBuilder.DropTable(
                name: "ScheduledJob");
        }
    }
}
