using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppliedPromotionId",
                table: "AppCarts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "AppCarts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PromoCodeUsed",
                table: "AppCarts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppPromotions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Promotion name"),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "Promotion description"),
                    Type = table.Column<int>(type: "int", nullable: false, comment: "Promotion type (Quantity, PromoCode, DateRange)"),
                    DisplayMode = table.Column<int>(type: "int", nullable: false, comment: "Display mode for customer notification"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Whether promotion is active"),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Promotion start date"),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Promotion end date"),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Priority for display (higher = shown first)"),
                    MinimumBoothsCount = table.Column<int>(type: "int", nullable: true, comment: "Minimum booths required"),
                    PromoCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "Promo code"),
                    RequiresPromoCode = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Whether promo code is required"),
                    DiscountType = table.Column<int>(type: "int", nullable: false, comment: "Discount type (Percentage or FixedAmount)"),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Discount value"),
                    MaxDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true, comment: "Max discount amount (for percentage)"),
                    MaxUsageCount = table.Column<int>(type: "int", nullable: true, comment: "Maximum total uses"),
                    CurrentUsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Current usage count"),
                    MaxUsagePerUser = table.Column<int>(type: "int", nullable: true, comment: "Maximum uses per user"),
                    CustomerMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Customer message (English)"),
                    CustomerMessagePl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Customer message (Polish)"),
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
                    table.PrimaryKey("PK_AppPromotions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppPromotionUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PromotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Promotion ID"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "User ID"),
                    CartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Cart ID"),
                    RentalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "Rental ID (optional)"),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Discount amount applied"),
                    PromoCodeUsed = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "Promo code used"),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Original cart amount"),
                    FinalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Final cart amount after discount"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPromotionUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPromotionUsages_AppCarts_CartId",
                        column: x => x.CartId,
                        principalTable: "AppCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppPromotionUsages_AppPromotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "AppPromotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppPromotionUsages_AppRentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "AppRentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Active_Validity",
                table: "AppPromotions",
                columns: new[] { "IsActive", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_IsActive",
                table: "AppPromotions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Priority",
                table: "AppPromotions",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_PromoCode",
                table: "AppPromotions",
                column: "PromoCode");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_TenantId",
                table: "AppPromotions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_TenantId_PromoCode",
                table: "AppPromotions",
                columns: new[] { "TenantId", "PromoCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Type",
                table: "AppPromotions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_CartId",
                table: "AppPromotionUsages",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_CreationTime",
                table: "AppPromotionUsages",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_PromotionId",
                table: "AppPromotionUsages",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_PromotionId_UserId",
                table: "AppPromotionUsages",
                columns: new[] { "PromotionId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_RentalId",
                table: "AppPromotionUsages",
                column: "RentalId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_UserId",
                table: "AppPromotionUsages",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppPromotionUsages");

            migrationBuilder.DropTable(
                name: "AppPromotions");

            migrationBuilder.DropColumn(
                name: "AppliedPromotionId",
                table: "AppCarts");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "AppCarts");

            migrationBuilder.DropColumn(
                name: "PromoCodeUsed",
                table: "AppCarts");
        }
    }
}
