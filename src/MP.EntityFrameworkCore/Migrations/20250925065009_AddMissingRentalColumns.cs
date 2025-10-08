using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingRentalColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BoothTypeId",
                table: "AppRentals",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Payment_PaymentStatus",
                table: "AppRentals",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Payment_Przelewy24TransactionId",
                table: "AppRentals",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppRentals_BoothTypeId",
                table: "AppRentals",
                column: "BoothTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppRentals_AppBoothTypes_BoothTypeId",
                table: "AppRentals",
                column: "BoothTypeId",
                principalTable: "AppBoothTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppRentals_AppBoothTypes_BoothTypeId",
                table: "AppRentals");

            migrationBuilder.DropIndex(
                name: "IX_AppRentals_BoothTypeId",
                table: "AppRentals");

            migrationBuilder.DropColumn(
                name: "BoothTypeId",
                table: "AppRentals");

            migrationBuilder.DropColumn(
                name: "Payment_PaymentStatus",
                table: "AppRentals");

            migrationBuilder.DropColumn(
                name: "Payment_Przelewy24TransactionId",
                table: "AppRentals");
        }
    }
}
