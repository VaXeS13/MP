using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicableBoothTypeIdsToPromotions : Migration
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicableBoothTypeIds",
                table: "AppPromotions");
        }
    }
}
