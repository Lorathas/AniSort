using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AniSort.Core.Migrations
{
    public partial class SchemaFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Job_LocalFiles_LocalFileId",
                table: "Job");

            migrationBuilder.DropForeignKey(
                name: "FK_Job_ScheduledJob_ScheduledJobId",
                table: "Job");

            migrationBuilder.DropForeignKey(
                name: "FK_JobLog_Job_JobId",
                table: "JobLog");

            migrationBuilder.DropForeignKey(
                name: "FK_JobStep_Job_JobId",
                table: "JobStep");

            migrationBuilder.DropForeignKey(
                name: "FK_StepLog_JobStep_StepId",
                table: "StepLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StepLog",
                table: "StepLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScheduledJob",
                table: "ScheduledJob");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JobStep",
                table: "JobStep");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JobLog",
                table: "JobLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Job",
                table: "Job");

            migrationBuilder.RenameTable(
                name: "StepLog",
                newName: "JobStepLogs");

            migrationBuilder.RenameTable(
                name: "ScheduledJob",
                newName: "ScheduledJobs");

            migrationBuilder.RenameTable(
                name: "JobStep",
                newName: "JobSteps");

            migrationBuilder.RenameTable(
                name: "JobLog",
                newName: "JobLogs");

            migrationBuilder.RenameTable(
                name: "Job",
                newName: "Jobs");

            migrationBuilder.RenameIndex(
                name: "IX_StepLog_StepId",
                table: "JobStepLogs",
                newName: "IX_JobStepLogs_StepId");

            migrationBuilder.RenameIndex(
                name: "IX_JobStep_JobId",
                table: "JobSteps",
                newName: "IX_JobSteps_JobId");

            migrationBuilder.RenameIndex(
                name: "IX_JobLog_JobId",
                table: "JobLogs",
                newName: "IX_JobLogs_JobId");

            migrationBuilder.RenameIndex(
                name: "IX_Job_ScheduledJobId",
                table: "Jobs",
                newName: "IX_Jobs_ScheduledJobId");

            migrationBuilder.RenameIndex(
                name: "IX_Job_LocalFileId",
                table: "Jobs",
                newName: "IX_Jobs_LocalFileId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JobStepLogs",
                table: "JobStepLogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScheduledJobs",
                table: "ScheduledJobs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JobSteps",
                table: "JobSteps",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JobLogs",
                table: "JobLogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Jobs",
                table: "Jobs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JobLogs_Jobs_JobId",
                table: "JobLogs",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_LocalFiles_LocalFileId",
                table: "Jobs",
                column: "LocalFileId",
                principalTable: "LocalFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_ScheduledJobs_ScheduledJobId",
                table: "Jobs",
                column: "ScheduledJobId",
                principalTable: "ScheduledJobs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JobStepLogs_JobSteps_StepId",
                table: "JobStepLogs",
                column: "StepId",
                principalTable: "JobSteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobSteps_Jobs_JobId",
                table: "JobSteps",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobLogs_Jobs_JobId",
                table: "JobLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_LocalFiles_LocalFileId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_ScheduledJobs_ScheduledJobId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_JobStepLogs_JobSteps_StepId",
                table: "JobStepLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_JobSteps_Jobs_JobId",
                table: "JobSteps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScheduledJobs",
                table: "ScheduledJobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JobSteps",
                table: "JobSteps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JobStepLogs",
                table: "JobStepLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Jobs",
                table: "Jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JobLogs",
                table: "JobLogs");

            migrationBuilder.RenameTable(
                name: "ScheduledJobs",
                newName: "ScheduledJob");

            migrationBuilder.RenameTable(
                name: "JobSteps",
                newName: "JobStep");

            migrationBuilder.RenameTable(
                name: "JobStepLogs",
                newName: "StepLog");

            migrationBuilder.RenameTable(
                name: "Jobs",
                newName: "Job");

            migrationBuilder.RenameTable(
                name: "JobLogs",
                newName: "JobLog");

            migrationBuilder.RenameIndex(
                name: "IX_JobSteps_JobId",
                table: "JobStep",
                newName: "IX_JobStep_JobId");

            migrationBuilder.RenameIndex(
                name: "IX_JobStepLogs_StepId",
                table: "StepLog",
                newName: "IX_StepLog_StepId");

            migrationBuilder.RenameIndex(
                name: "IX_Jobs_ScheduledJobId",
                table: "Job",
                newName: "IX_Job_ScheduledJobId");

            migrationBuilder.RenameIndex(
                name: "IX_Jobs_LocalFileId",
                table: "Job",
                newName: "IX_Job_LocalFileId");

            migrationBuilder.RenameIndex(
                name: "IX_JobLogs_JobId",
                table: "JobLog",
                newName: "IX_JobLog_JobId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScheduledJob",
                table: "ScheduledJob",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JobStep",
                table: "JobStep",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StepLog",
                table: "StepLog",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Job",
                table: "Job",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JobLog",
                table: "JobLog",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Job_LocalFiles_LocalFileId",
                table: "Job",
                column: "LocalFileId",
                principalTable: "LocalFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Job_ScheduledJob_ScheduledJobId",
                table: "Job",
                column: "ScheduledJobId",
                principalTable: "ScheduledJob",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JobLog_Job_JobId",
                table: "JobLog",
                column: "JobId",
                principalTable: "Job",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobStep_Job_JobId",
                table: "JobStep",
                column: "JobId",
                principalTable: "Job",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StepLog_JobStep_StepId",
                table: "StepLog",
                column: "StepId",
                principalTable: "JobStep",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
