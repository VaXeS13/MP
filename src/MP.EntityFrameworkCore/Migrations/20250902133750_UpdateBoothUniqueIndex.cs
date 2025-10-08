using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBoothUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Booths_Number",
                table: "AppBooths");

            migrationBuilder.CreateIndex(
                name: "IX_Booths_TenantId_Number",
                table: "AppBooths",
                columns: new[] { "TenantId", "Number" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Booths_TenantId_Number",
                table: "AppBooths");

            migrationBuilder.CreateIndex(
                name: "IX_Booths_Number",
                table: "AppBooths",
                column: "Number",
                unique: true);
        }
    }
}
