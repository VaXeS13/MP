using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddBoothPricingPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerDay",
                table: "AppBooths",
                type: "decimal(18,2)",
                nullable: false,
                comment: "Cena za dzień (legacy - use PricingPeriods)",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldComment: "Cena za dzień");

            migrationBuilder.CreateTable(
                name: "AppBoothPricingPeriods",
                columns: table => new
                {
                    BoothId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "FK to Booth"),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Days = table.Column<int>(type: "int", nullable: false, comment: "Number of days in pricing period"),
                    PricePerPeriod = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Price for this period")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBoothPricingPeriods", x => new { x.BoothId, x.Id });
                    table.ForeignKey(
                        name: "FK_AppBoothPricingPeriods_AppBooths_BoothId",
                        column: x => x.BoothId,
                        principalTable: "AppBooths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoothPricingPeriods_BoothId",
                table: "AppBoothPricingPeriods",
                column: "BoothId");

            migrationBuilder.CreateIndex(
                name: "IX_BoothPricingPeriods_BoothId_Days",
                table: "AppBoothPricingPeriods",
                columns: new[] { "BoothId", "Days" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppBoothPricingPeriods");

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerDay",
                table: "AppBooths",
                type: "decimal(18,2)",
                nullable: false,
                comment: "Cena za dzień",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldComment: "Cena za dzień (legacy - use PricingPeriods)");
        }
    }
}
