using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaSPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupportEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaintenanceMode = table.Column<bool>(type: "bit", nullable: false),
                    AllowRegistrations = table.Column<bool>(type: "bit", nullable: false),
                    MfaRequired = table.Column<bool>(type: "bit", nullable: false),
                    SessionTimeout = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformSettings");
        }
    }
}
