using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRentalItemsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppSettlementItems_AppRentalItems_RentalItemId",
                table: "AppSettlementItems");

            migrationBuilder.DropTable(
                name: "AppRentalItems");

            migrationBuilder.DropIndex(
                name: "IX_SettlementItems_RentalItemId",
                table: "AppSettlementItems");

            migrationBuilder.DropColumn(
                name: "RentalItemId",
                table: "AppSettlementItems");

            migrationBuilder.AddColumn<Guid>(
                name: "ItemSheetItemId",
                table: "AppSettlementItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "ID przedmiotu z arkusza");

            migrationBuilder.AddColumn<Guid>(
                name: "RentalId1",
                table: "AppItemSheets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SettlementItems_ItemSheetItemId",
                table: "AppSettlementItems",
                column: "ItemSheetItemId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_AppSettlementItems_AppItemSheetItems_ItemSheetItemId",
                table: "AppSettlementItems",
                column: "ItemSheetItemId",
                principalTable: "AppItemSheetItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppItemSheets_AppRentals_RentalId1",
                table: "AppItemSheets");

            migrationBuilder.DropForeignKey(
                name: "FK_AppSettlementItems_AppItemSheetItems_ItemSheetItemId",
                table: "AppSettlementItems");

            migrationBuilder.DropIndex(
                name: "IX_SettlementItems_ItemSheetItemId",
                table: "AppSettlementItems");

            migrationBuilder.DropIndex(
                name: "IX_AppItemSheets_RentalId1",
                table: "AppItemSheets");

            migrationBuilder.DropColumn(
                name: "ItemSheetItemId",
                table: "AppSettlementItems");

            migrationBuilder.DropColumn(
                name: "RentalId1",
                table: "AppItemSheets");

            migrationBuilder.AddColumn<Guid>(
                name: "RentalItemId",
                table: "AppSettlementItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "ID przedmiotu z wynajmu");

            migrationBuilder.CreateTable(
                name: "AppRentalItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RentalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActualPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CommissionPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EstimatedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExpirationNotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ItemNumber = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoldAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRentalItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppRentalItems_AppRentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "AppRentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SettlementItems_RentalItemId",
                table: "AppSettlementItems",
                column: "RentalItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppRentalItems_Barcode",
                table: "AppRentalItems",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_AppRentalItems_Category",
                table: "AppRentalItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AppRentalItems_ItemNumber",
                table: "AppRentalItems",
                column: "ItemNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AppRentalItems_RentalId",
                table: "AppRentalItems",
                column: "RentalId");

            migrationBuilder.CreateIndex(
                name: "IX_AppRentalItems_Status",
                table: "AppRentalItems",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_AppSettlementItems_AppRentalItems_RentalItemId",
                table: "AppSettlementItems",
                column: "RentalItemId",
                principalTable: "AppRentalItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
