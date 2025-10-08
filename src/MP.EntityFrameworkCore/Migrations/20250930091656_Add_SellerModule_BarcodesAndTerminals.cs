using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class Add_SellerModule_BarcodesAndTerminals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "AppRentalItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppTenantTerminalSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "Terminal provider identifier (e.g., ingenico, verifone, stripe_terminal, mock)"),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Whether this terminal configuration is enabled"),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false, defaultValue: "{}", comment: "Provider-specific configuration JSON"),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "PLN", comment: "Currency code for terminal transactions"),
                    Region = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "Region/country code"),
                    IsSandbox = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Whether this is a sandbox/test configuration"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTenantTerminalSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppRentalItems_Barcode",
                table: "AppRentalItems",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_TenantTerminalSettings_ProviderId",
                table: "AppTenantTerminalSettings",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantTerminalSettings_TenantId",
                table: "AppTenantTerminalSettings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantTerminalSettings_TenantId_IsEnabled",
                table: "AppTenantTerminalSettings",
                columns: new[] { "TenantId", "IsEnabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppTenantTerminalSettings");

            migrationBuilder.DropIndex(
                name: "IX_AppRentalItems_Barcode",
                table: "AppRentalItems");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "AppRentalItems");
        }
    }
}
