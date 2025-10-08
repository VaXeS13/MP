using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class Add_ItemNumber_AutoBarcodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItemNumber",
                table: "AppRentalItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AppRentalItems_ItemNumber",
                table: "AppRentalItems",
                column: "ItemNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppRentalItems_ItemNumber",
                table: "AppRentalItems");

            migrationBuilder.DropColumn(
                name: "ItemNumber",
                table: "AppRentalItems");
        }
    }
}
