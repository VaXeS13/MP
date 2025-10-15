using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddBankAccountNumberToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "AbpUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "AbpUsers",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AppHomePageSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SectionType = table.Column<int>(type: "int", nullable: false, comment: "Type of homepage section"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Section title"),
                    Subtitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Section subtitle"),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true, comment: "HTML content for the section"),
                    ImageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true, comment: "URL to the main image"),
                    LinkUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true, comment: "URL for CTA link"),
                    LinkText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Text for CTA button/link"),
                    Order = table.Column<int>(type: "int", nullable: false, comment: "Display order (lower = displayed first)"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Whether the section is active/published"),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Start date for scheduled publishing"),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "End date for scheduled publishing"),
                    BackgroundColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "Background color (hex code)"),
                    TextColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "Text color (hex code)"),
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
                    table.PrimaryKey("PK_AppHomePageSections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomePageSections_Active_Validity",
                table: "AppHomePageSections",
                columns: new[] { "IsActive", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_HomePageSections_IsActive",
                table: "AppHomePageSections",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_HomePageSections_Order",
                table: "AppHomePageSections",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_HomePageSections_SectionType",
                table: "AppHomePageSections",
                column: "SectionType");

            migrationBuilder.CreateIndex(
                name: "IX_HomePageSections_TenantId",
                table: "AppHomePageSections",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HomePageSections_TenantId_IsActive_Order",
                table: "AppHomePageSections",
                columns: new[] { "TenantId", "IsActive", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppHomePageSections");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "AbpUsers");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "AbpUsers");
        }
    }
}
