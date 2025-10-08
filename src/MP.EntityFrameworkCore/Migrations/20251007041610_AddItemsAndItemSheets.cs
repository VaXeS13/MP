using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddItemsAndItemSheets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID użytkownika - właściciela przedmiotu"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nazwa przedmiotu"),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Kategoria przedmiotu"),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Cena przedmiotu"),
                    Currency = table.Column<int>(type: "int", nullable: false, comment: "Waluta ceny"),
                    Status = table.Column<int>(type: "int", nullable: false, comment: "Status przedmiotu"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppItems_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppItemSheets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID użytkownika - właściciela arkusza"),
                    RentalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "ID wynajmu (nullable - może być nieprzypisany)"),
                    Status = table.Column<int>(type: "int", nullable: false, comment: "Status arkusza"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppItemSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppItemSheets_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppItemSheets_AppRentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "AppRentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AppItemSheetItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemSheetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID arkusza"),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID przedmiotu"),
                    ItemNumber = table.Column<int>(type: "int", nullable: false, comment: "Numer pozycji w arkuszu"),
                    Barcode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "Barcode (Base36 z Guid)"),
                    CommissionPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false, comment: "Procent prowizji"),
                    Status = table.Column<int>(type: "int", nullable: false, comment: "Status pozycji"),
                    SoldAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Data sprzedaży"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppItemSheetItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppItemSheetItems_AppItemSheets_ItemSheetId",
                        column: x => x.ItemSheetId,
                        principalTable: "AppItemSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppItemSheetItems_AppItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "AppItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_Category",
                table: "AppItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Status",
                table: "AppItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Items_UserId",
                table: "AppItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_UserId_Status",
                table: "AppItems",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemSheetItems_Barcode",
                table: "AppItemSheetItems",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSheetItems_ItemId",
                table: "AppItemSheetItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSheetItems_ItemSheetId",
                table: "AppItemSheetItems",
                column: "ItemSheetId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSheetItems_ItemSheetId_ItemNumber",
                table: "AppItemSheetItems",
                columns: new[] { "ItemSheetId", "ItemNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemSheetItems_Status",
                table: "AppItemSheetItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSheets_RentalId",
                table: "AppItemSheets",
                column: "RentalId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSheets_Status",
                table: "AppItemSheets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSheets_UserId",
                table: "AppItemSheets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemSheets_UserId_Status",
                table: "AppItemSheets",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppItemSheetItems");

            migrationBuilder.DropTable(
                name: "AppItemSheets");

            migrationBuilder.DropTable(
                name: "AppItems");
        }
    }
}
