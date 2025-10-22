using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceBreakdownToRental : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PriceBreakdown",
                table: "AppRentals",
                type: "nvarchar(max)",
                nullable: true,
                comment: "Price breakdown JSON showing how total price was calculated");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceBreakdown",
                table: "AppRentals");
        }
    }
}
