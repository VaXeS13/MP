using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class StartEndDateFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rentals_Period",
                table: "AppRentals");

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_Period",
                table: "AppRentals",
                columns: new[] { "StartDate", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rentals_Period",
                table: "AppRentals");

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_Period",
                table: "AppRentals",
                columns: new[] { "StartedAt", "CompletedAt" });
        }
    }
}
