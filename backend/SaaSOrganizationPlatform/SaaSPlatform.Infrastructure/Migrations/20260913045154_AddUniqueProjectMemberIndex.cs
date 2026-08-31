using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaSPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueProjectMemberIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_ProjectId",
                table: "ProjectMembers");

            // Remove any duplicate memberships that predate the unique constraint,
            // keeping the earliest row per (ProjectId, UserId) combination.
            migrationBuilder.Sql(
                "DELETE pm FROM ProjectMembers pm " +
                "INNER JOIN ProjectMembers keep ON keep.ProjectId = pm.ProjectId " +
                "AND keep.UserId = pm.UserId AND keep.Id < pm.Id;");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "TaskItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId_UserId",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectMembers_ProjectId_UserId",
                table: "ProjectMembers");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "TaskItems");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId",
                table: "ProjectMembers",
                column: "ProjectId");
        }
    }
}
