using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCustomerMessagePlFromPromotion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerMessagePl",
                table: "AppPromotions");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerMessage",
                table: "AppPromotions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                comment: "Customer message",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "Customer message (English)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CustomerMessage",
                table: "AppPromotions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                comment: "Customer message (English)",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "Customer message");

            migrationBuilder.AddColumn<string>(
                name: "CustomerMessagePl",
                table: "AppPromotions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                comment: "Customer message (Polish)");
        }
    }
}
