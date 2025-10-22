using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionDetailsToRental : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppliedPromotionId",
                table: "AppRentals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "AppRentals",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PromoCodeUsed",
                table: "AppRentals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountPercentage",
                table: "AppCartItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Discount percentage applied to this item",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountAmount",
                table: "AppCartItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Discount amount applied to this item",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppliedPromotionId",
                table: "AppRentals");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "AppRentals");

            migrationBuilder.DropColumn(
                name: "PromoCodeUsed",
                table: "AppRentals");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountPercentage",
                table: "AppCartItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldDefaultValue: 0m,
                oldComment: "Discount percentage applied to this item");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountAmount",
                table: "AppCartItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldDefaultValue: 0m,
                oldComment: "Discount amount applied to this item");
        }
    }
}
