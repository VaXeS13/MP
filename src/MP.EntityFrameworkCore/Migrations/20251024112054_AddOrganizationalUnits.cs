using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationalUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppFloorPlans",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppBoothTypes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppBooths",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AppOrganizationalUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Display name of the organizational unit"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "Unique code within tenant (e.g., MAIN, WARSAW-CENTER)"),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Street address"),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "City location"),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "Postal code"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "Contact email"),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "Contact phone"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Whether unit is active"),
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
                    table.PrimaryKey("PK_AppOrganizationalUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppOrgUnitRegistrationCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationalUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "The organizational unit this code is for"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Unique registration code (format: TENANT-UNIT-RANDOM)"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "Optional role to auto-assign when code is used"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Optional expiration date"),
                    MaxUsageCount = table.Column<int>(type: "int", nullable: true, comment: "Optional max usage limit"),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Current usage count"),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "When code was last used"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Whether code is active"),
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
                    table.PrimaryKey("PK_AppOrgUnitRegistrationCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppOrgUnitRegistrationCodes_AppOrganizationalUnits_OrganizationalUnitId",
                        column: x => x.OrganizationalUnitId,
                        principalTable: "AppOrganizationalUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppOrgUnitSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationalUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "The organizational unit these settings apply to"),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "PLN", comment: "Primary currency for the unit (PLN, EUR, USD, GBP, CZK)"),
                    EnabledPaymentProviders = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: "{}", comment: "JSON configuration of enabled payment providers"),
                    DefaultPaymentProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "Default payment provider for unit (stripe, p24, paypal)"),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Optional logo URL for branding"),
                    BannerText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "Optional banner text"),
                    IsMainUnit = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Whether this is the primary unit for tenant"),
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
                    table.PrimaryKey("PK_AppOrgUnitSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppOrgUnitSettings_AppOrganizationalUnits_OrganizationalUnitId",
                        column: x => x.OrganizationalUnitId,
                        principalTable: "AppOrganizationalUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppUserOrganizationalUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "User ID"),
                    OrganizationalUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Organizational unit ID"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "Optional unit-specific role ID"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Whether assignment is active"),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "When user was assigned to unit")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserOrganizationalUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUserOrganizationalUnits_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppUserOrganizationalUnits_AppOrganizationalUnits_OrganizationalUnitId",
                        column: x => x.OrganizationalUnitId,
                        principalTable: "AppOrganizationalUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalUnits_IsActive",
                table: "AppOrganizationalUnits",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalUnits_TenantId_Code",
                table: "AppOrganizationalUnits",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppOrgUnitRegistrationCodes_OrganizationalUnitId",
                table: "AppOrgUnitRegistrationCodes",
                column: "OrganizationalUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnitRegCodes_ExpiresAt",
                table: "AppOrgUnitRegistrationCodes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnitRegCodes_IsActive",
                table: "AppOrgUnitRegistrationCodes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnitRegCodes_TenantId_Code",
                table: "AppOrgUnitRegistrationCodes",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnitRegCodes_TenantId_OrgUnitId",
                table: "AppOrgUnitRegistrationCodes",
                columns: new[] { "TenantId", "OrganizationalUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppOrgUnitSettings_OrganizationalUnitId",
                table: "AppOrgUnitSettings",
                column: "OrganizationalUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnitSettings_TenantId_OrgUnitId",
                table: "AppOrgUnitSettings",
                columns: new[] { "TenantId", "OrganizationalUnitId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserOrganizationalUnits_OrganizationalUnitId",
                table: "AppUserOrganizationalUnits",
                column: "OrganizationalUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserOrganizationalUnits_UserId",
                table: "AppUserOrganizationalUnits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrgUnits_IsActive",
                table: "AppUserOrganizationalUnits",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrgUnits_TenantId_OrgUnitId",
                table: "AppUserOrganizationalUnits",
                columns: new[] { "TenantId", "OrganizationalUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserOrgUnits_TenantId_UserId_OrgUnitId",
                table: "AppUserOrganizationalUnits",
                columns: new[] { "TenantId", "UserId", "OrganizationalUnitId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppOrgUnitRegistrationCodes");

            migrationBuilder.DropTable(
                name: "AppOrgUnitSettings");

            migrationBuilder.DropTable(
                name: "AppUserOrganizationalUnits");

            migrationBuilder.DropTable(
                name: "AppOrganizationalUnits");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppFloorPlans");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppBoothTypes");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppBooths");
        }
    }
}
