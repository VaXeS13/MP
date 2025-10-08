using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class FixBoothAndItemSheetRentalRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppItemSheets_AppRentals_RentalId1",
                table: "AppItemSheets");

            migrationBuilder.DropIndex(
                name: "IX_AppItemSheets_RentalId1",
                table: "AppItemSheets");

            migrationBuilder.DropColumn(
                name: "RentalId1",
                table: "AppItemSheets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RentalId1",
                table: "AppItemSheets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppItemSheets_RentalId1",
                table: "AppItemSheets",
                column: "RentalId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AppItemSheets_AppRentals_RentalId1",
                table: "AppItemSheets",
                column: "RentalId1",
                principalTable: "AppRentals",
                principalColumn: "Id");
        }
    }
}
