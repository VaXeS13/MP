using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddSettlementPaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "AppSettlements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentProviderMetadata",
                table: "AppSettlements",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "AppSettlements");

            migrationBuilder.DropColumn(
                name: "PaymentProviderMetadata",
                table: "AppSettlements");
        }
    }
}
