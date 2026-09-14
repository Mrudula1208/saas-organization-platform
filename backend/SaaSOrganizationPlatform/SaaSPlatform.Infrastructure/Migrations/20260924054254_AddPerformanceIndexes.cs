using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaSPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskItems_ProjectId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_Projects_TenantId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TenantId",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TaskItems",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ProjectId_Status_CreatedAt",
                table: "TaskItems",
                columns: new[] { "ProjectId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_TenantId_Status_CreatedAt",
                table: "TaskItems",
                columns: new[] { "TenantId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_TenantId_CreatedAt",
                table: "SystemLogs",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TenantId_IsDeleted_CreatedAt",
                table: "Projects",
                columns: new[] { "TenantId", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_PaymentDate",
                table: "Payments",
                columns: new[] { "TenantId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "TenantId", "IsRead", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_ProjectId_Status_CreatedAt",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_TenantId_Status_CreatedAt",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_SystemLogs_TenantId_CreatedAt",
                table: "SystemLogs");

            migrationBuilder.DropIndex(
                name: "IX_Projects_TenantId_IsDeleted_CreatedAt",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId_PaymentDate",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TenantId_IsRead_CreatedAt",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TaskItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ProjectId",
                table: "TaskItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TenantId",
                table: "Projects",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId",
                table: "Payments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId",
                table: "Notifications",
                column: "TenantId");
        }
    }
}
