using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddRentalExtensionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Payment_PaymentMethod",
                table: "AppRentals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Payment_TerminalReceiptNumber",
                table: "AppRentals",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Payment_TerminalTransactionId",
                table: "AppRentals",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExtensionTimeoutAt",
                table: "AppCarts",
                type: "datetime2",
                nullable: true,
                comment: "Czas wygaśnięcia koszyka przy przedłużeniu online");

            migrationBuilder.AddColumn<Guid>(
                name: "ExtendedRentalId",
                table: "AppCartItems",
                type: "uniqueidentifier",
                nullable: true,
                comment: "ID wynajmu który jest przedłużany (dla ItemType=Extension)");

            migrationBuilder.AddColumn<int>(
                name: "ItemType",
                table: "AppCartItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "Typ pozycji w koszyku (Rental/Extension)");

            migrationBuilder.CreateTable(
                name: "AppRentalExtensionPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RentalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID wynajmu"),
                    OldEndDate = table.Column<DateTime>(type: "date", nullable: false, comment: "Poprzednia data zakończenia"),
                    NewEndDate = table.Column<DateTime>(type: "date", nullable: false, comment: "Nowa data zakończenia"),
                    ExtensionCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Koszt przedłużenia"),
                    PaymentType = table.Column<int>(type: "int", nullable: false, comment: "Typ płatności za przedłużenie"),
                    ExtendedAt = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Data przedłużenia"),
                    ExtendedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID użytkownika który wykonał przedłużenie"),
                    TransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "ID transakcji (dla Terminal/Online)"),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Numer paragonu (dla Terminal)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRentalExtensionPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppRentalExtensionPayments_AppRentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "AppRentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Carts_ExtensionTimeoutAt",
                table: "AppCarts",
                column: "ExtensionTimeoutAt");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ExtendedRentalId",
                table: "AppCartItems",
                column: "ExtendedRentalId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ItemType",
                table: "AppCartItems",
                column: "ItemType");

            migrationBuilder.CreateIndex(
                name: "IX_RentalExtensionPayments_ExtendedAt",
                table: "AppRentalExtensionPayments",
                column: "ExtendedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RentalExtensionPayments_ExtendedBy",
                table: "AppRentalExtensionPayments",
                column: "ExtendedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RentalExtensionPayments_PaymentType",
                table: "AppRentalExtensionPayments",
                column: "PaymentType");

            migrationBuilder.CreateIndex(
                name: "IX_RentalExtensionPayments_RentalId",
                table: "AppRentalExtensionPayments",
                column: "RentalId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppCartItems_AppRentals_ExtendedRentalId",
                table: "AppCartItems",
                column: "ExtendedRentalId",
                principalTable: "AppRentals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppCartItems_AppRentals_ExtendedRentalId",
                table: "AppCartItems");

            migrationBuilder.DropTable(
                name: "AppRentalExtensionPayments");

            migrationBuilder.DropIndex(
                name: "IX_Carts_ExtensionTimeoutAt",
                table: "AppCarts");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_ExtendedRentalId",
                table: "AppCartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_ItemType",
                table: "AppCartItems");

            migrationBuilder.DropColumn(
                name: "Payment_PaymentMethod",
                table: "AppRentals");

            migrationBuilder.DropColumn(
                name: "Payment_TerminalReceiptNumber",
                table: "AppRentals");

            migrationBuilder.DropColumn(
                name: "Payment_TerminalTransactionId",
                table: "AppRentals");

            migrationBuilder.DropColumn(
                name: "ExtensionTimeoutAt",
                table: "AppCarts");

            migrationBuilder.DropColumn(
                name: "ExtendedRentalId",
                table: "AppCartItems");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "AppCartItems");
        }
    }
}
