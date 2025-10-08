using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class FixDefaultCurrencyValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update all existing booths with Currency = 0 to PLN = 1
            migrationBuilder.Sql("UPDATE AppBooths SET Currency = 1 WHERE Currency = 0");

            // Update default value for new booths to PLN = 1
            migrationBuilder.AlterColumn<int>(
                name: "Currency",
                table: "AppBooths",
                type: "int",
                nullable: false,
                defaultValue: 1,
                comment: "Waluta stanowiska",
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0,
                oldComment: "Waluta stanowiska");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert default value back to 0
            migrationBuilder.AlterColumn<int>(
                name: "Currency",
                table: "AppBooths",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "Waluta stanowiska",
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1,
                oldComment: "Waluta stanowiska");
        }
    }
}
