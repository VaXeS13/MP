using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicableBoothIdsToPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicableBoothTypeIds",
                table: "AppPromotions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]",
                comment: "List of booth type IDs this promotion applies to (empty = all types)");

            migrationBuilder.AddColumn<string>(
                name: "ApplicableBoothIds",
                table: "AppPromotions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]",
                comment: "List of specific booth IDs this promotion applies to (empty = all booths)");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_TenantId_IsActive",
                table: "AppPromotions",
                columns: new[] { "TenantId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Promotions_TenantId_IsActive",
                table: "AppPromotions");

            migrationBuilder.DropColumn(
                name: "ApplicableBoothIds",
                table: "AppPromotions");
        }
    }
}
