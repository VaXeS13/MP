using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class MoveCurrencyFromBoothToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "AppBooths");

            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "AppRentals",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "Waluta wynajmu (snapshot at checkout)");

            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "AppRentalExtensionPayments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "Waluta przedłużenia (snapshot)");

            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "AppCartItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "Waluta (snapshot from tenant)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "AppRentals");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "AppRentalExtensionPayments");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "AppCartItems");

            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "AppBooths",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "Waluta stanowiska");
        }
    }
}
