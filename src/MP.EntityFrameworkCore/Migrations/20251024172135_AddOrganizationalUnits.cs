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
            migrationBuilder.DropIndex(
                name: "IX_PromotionUsages_PromotionId_UserId",
                table: "AppPromotionUsages");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_TenantId_IsActive",
                table: "AppPromotions");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_TenantId_PromoCode",
                table: "AppPromotions");

            migrationBuilder.DropIndex(
                name: "IX_HomePageSections_TenantId_IsActive_Order",
                table: "AppHomePageSections");

            migrationBuilder.DropIndex(
                name: "IX_FloorPlans_TenantId_Level",
                table: "AppFloorPlans");

            migrationBuilder.DropIndex(
                name: "IX_FloorPlans_TenantId_Name",
                table: "AppFloorPlans");

            migrationBuilder.DropIndex(
                name: "IX_BoothTypes_TenantId_Name",
                table: "AppBoothTypes");

            migrationBuilder.DropIndex(
                name: "IX_Booths_TenantId_Number",
                table: "AppBooths");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppUserNotifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppUploadedFiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppTenantTerminalSettings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppTenantFiscalPrinterSettings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppStripeTransactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppSettlements",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppRentals",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppRentalExtensionPayments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppPromotionUsages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppPromotions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppPayPalTransactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppP24Transactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppItemSheets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppHomePageSections",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppFloorPlans",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppChatMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppCarts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppCartItems",
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

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationalUnitId",
                table: "AppAgentApiKeys",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AppOrganizationalUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Organization unit name"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "Organization unit code (unique per tenant)"),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true, comment: "Physical address"),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "City"),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "Postal code"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "Contact email"),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "Contact phone number"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Whether the organizational unit is active"),
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
                name: "AppOrganizationalUnitRegistrationCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationalUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Organizational unit ID"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "Registration code"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "Role ID to assign upon registration (optional)"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "When this registration code expires"),
                    MaxUsageCount = table.Column<int>(type: "int", nullable: true, comment: "Maximum number of uses (null = unlimited)"),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Current usage count"),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "When this code was last used"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Whether this code is active"),
                    OrganizationalUnitId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_AppOrganizationalUnitRegistrationCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppOrganizationalUnitRegistrationCodes_AppOrganizationalUnits_OrganizationalUnitId",
                        column: x => x.OrganizationalUnitId,
                        principalTable: "AppOrganizationalUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppOrganizationalUnitRegistrationCodes_AppOrganizationalUnits_OrganizationalUnitId1",
                        column: x => x.OrganizationalUnitId1,
                        principalTable: "AppOrganizationalUnits",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppOrganizationalUnitSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationalUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Organizational unit ID"),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "PLN", comment: "Default currency for this unit (PLN, EUR, USD, GBP, CZK)"),
                    EnabledPaymentProviders = table.Column<string>(type: "nvarchar(max)", nullable: false, comment: "JSON array of enabled payment provider IDs (Przelewy24, Stripe, PayPal)"),
                    DefaultPaymentProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "Default payment provider ID"),
                    LogoUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true, comment: "URL to the organization logo"),
                    BannerText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Custom banner text for the organization"),
                    IsMainUnit = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Whether this is the main organizational unit"),
                    OrganizationalUnitId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_AppOrganizationalUnitSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppOrganizationalUnitSettings_AppOrganizationalUnits_OrganizationalUnitId",
                        column: x => x.OrganizationalUnitId,
                        principalTable: "AppOrganizationalUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppOrganizationalUnitSettings_AppOrganizationalUnits_OrganizationalUnitId1",
                        column: x => x.OrganizationalUnitId1,
                        principalTable: "AppOrganizationalUnits",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppUserOrganizationalUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "User ID"),
                    OrganizationalUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Organizational unit ID"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "Role ID (optional)"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Whether this assignment is active"),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "When the user was assigned to this unit"),
                    AppUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationalUnitId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserOrganizationalUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUserOrganizationalUnits_AbpUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppUserOrganizationalUnits_AppOrganizationalUnits_OrganizationalUnitId",
                        column: x => x.OrganizationalUnitId,
                        principalTable: "AppOrganizationalUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppUserOrganizationalUnits_AppOrganizationalUnits_OrganizationalUnitId1",
                        column: x => x.OrganizationalUnitId1,
                        principalTable: "AppOrganizationalUnits",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_TenantId_OrgUnit_PromotionId_UserId",
                table: "AppPromotionUsages",
                columns: new[] { "TenantId", "OrganizationalUnitId", "PromotionId", "UserId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_TenantId_OrgUnit_IsActive",
                table: "AppPromotions",
                columns: new[] { "TenantId", "OrganizationalUnitId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_TenantId_OrgUnit_PromoCode",
                table: "AppPromotions",
                columns: new[] { "TenantId", "OrganizationalUnitId", "PromoCode" },
                unique: true,
                filter: "[TenantId] IS NOT NULL AND [PromoCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HomePageSections_TenantId_OrgUnit_IsActive_Order",
                table: "AppHomePageSections",
                columns: new[] { "TenantId", "OrganizationalUnitId", "IsActive", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlans_TenantId_OrgUnit_Level",
                table: "AppFloorPlans",
                columns: new[] { "TenantId", "OrganizationalUnitId", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlans_TenantId_OrgUnit_Name",
                table: "AppFloorPlans",
                columns: new[] { "TenantId", "OrganizationalUnitId", "Name" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BoothTypes_TenantId_OrgUnit_Name",
                table: "AppBoothTypes",
                columns: new[] { "TenantId", "OrganizationalUnitId", "Name" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Booths_TenantId_OrgUnit_Number",
                table: "AppBooths",
                columns: new[] { "TenantId", "OrganizationalUnitId", "Number" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppOrganizationalUnitRegistrationCodes_OrganizationalUnitId1",
                table: "AppOrganizationalUnitRegistrationCodes",
                column: "OrganizationalUnitId1");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalUnitRegistrationCodes_ExpiresAt",
                table: "AppOrganizationalUnitRegistrationCodes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalUnitRegistrationCodes_IsActive",
                table: "AppOrganizationalUnitRegistrationCodes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalUnitRegistrationCodes_OrganizationalUnitId",
                table: "AppOrganizationalUnitRegistrationCodes",
                column: "OrganizationalUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalUnitRegistrationCodes_TenantId_Code",
                table: "AppOrganizationalUnitRegistrationCodes",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

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
                name: "IX_AppOrganizationalUnitSettings_OrganizationalUnitId",
                table: "AppOrganizationalUnitSettings",
                column: "OrganizationalUnitId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppOrganizationalUnitSettings_OrganizationalUnitId1",
                table: "AppOrganizationalUnitSettings",
                column: "OrganizationalUnitId1");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalUnitSettings_TenantId_OrgUnit",
                table: "AppOrganizationalUnitSettings",
                columns: new[] { "TenantId", "OrganizationalUnitId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserOrganizationalUnits_AppUserId",
                table: "AppUserOrganizationalUnits",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserOrganizationalUnits_OrganizationalUnitId1",
                table: "AppUserOrganizationalUnits",
                column: "OrganizationalUnitId1");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganizationalUnits_IsActive",
                table: "AppUserOrganizationalUnits",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganizationalUnits_OrganizationalUnitId",
                table: "AppUserOrganizationalUnits",
                column: "OrganizationalUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganizationalUnits_TenantId_UserId_OrgUnit",
                table: "AppUserOrganizationalUnits",
                columns: new[] { "TenantId", "UserId", "OrganizationalUnitId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppOrganizationalUnitRegistrationCodes");

            migrationBuilder.DropTable(
                name: "AppOrganizationalUnitSettings");

            migrationBuilder.DropTable(
                name: "AppUserOrganizationalUnits");

            migrationBuilder.DropTable(
                name: "AppOrganizationalUnits");

            migrationBuilder.DropIndex(
                name: "IX_PromotionUsages_TenantId_OrgUnit_PromotionId_UserId",
                table: "AppPromotionUsages");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_TenantId_OrgUnit_IsActive",
                table: "AppPromotions");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_TenantId_OrgUnit_PromoCode",
                table: "AppPromotions");

            migrationBuilder.DropIndex(
                name: "IX_HomePageSections_TenantId_OrgUnit_IsActive_Order",
                table: "AppHomePageSections");

            migrationBuilder.DropIndex(
                name: "IX_FloorPlans_TenantId_OrgUnit_Level",
                table: "AppFloorPlans");

            migrationBuilder.DropIndex(
                name: "IX_FloorPlans_TenantId_OrgUnit_Name",
                table: "AppFloorPlans");

            migrationBuilder.DropIndex(
                name: "IX_BoothTypes_TenantId_OrgUnit_Name",
                table: "AppBoothTypes");

            migrationBuilder.DropIndex(
                name: "IX_Booths_TenantId_OrgUnit_Number",
                table: "AppBooths");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppUserNotifications");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppUploadedFiles");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppTenantTerminalSettings");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppTenantFiscalPrinterSettings");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppStripeTransactions");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppSettlements");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppRentals");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppRentalExtensionPayments");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppPromotionUsages");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppPromotions");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppPayPalTransactions");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppP24Transactions");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppItemSheets");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppItems");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppHomePageSections");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppFloorPlans");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppChatMessages");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppCarts");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppCartItems");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppBoothTypes");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppBooths");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "AppAgentApiKeys");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_PromotionId_UserId",
                table: "AppPromotionUsages",
                columns: new[] { "PromotionId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_TenantId_IsActive",
                table: "AppPromotions",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_TenantId_PromoCode",
                table: "AppPromotions",
                columns: new[] { "TenantId", "PromoCode" });

            migrationBuilder.CreateIndex(
                name: "IX_HomePageSections_TenantId_IsActive_Order",
                table: "AppHomePageSections",
                columns: new[] { "TenantId", "IsActive", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlans_TenantId_Level",
                table: "AppFloorPlans",
                columns: new[] { "TenantId", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlans_TenantId_Name",
                table: "AppFloorPlans",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BoothTypes_TenantId_Name",
                table: "AppBoothTypes",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Booths_TenantId_Number",
                table: "AppBooths",
                columns: new[] { "TenantId", "Number" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }
    }
}
