using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddCartTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppCarts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID użytkownika"),
                    Status = table.Column<int>(type: "int", nullable: false, comment: "Status koszyka"),
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
                    table.PrimaryKey("PK_AppCarts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCarts_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppCartItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID koszyka"),
                    BoothId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID stanowiska"),
                    BoothTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID typu stanowiska"),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false, comment: "Data rozpoczęcia wynajmu"),
                    EndDate = table.Column<DateTime>(type: "date", nullable: false, comment: "Data zakończenia wynajmu"),
                    PricePerDay = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Cena za dzień"),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "Notatki do wynajmu"),
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
                    table.PrimaryKey("PK_AppCartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCartItems_AppCarts_CartId",
                        column: x => x.CartId,
                        principalTable: "AppCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_Booth_Period",
                table: "AppCartItems",
                columns: new[] { "BoothId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_BoothId",
                table: "AppCartItems",
                column: "BoothId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "AppCartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_CreationTime",
                table: "AppCarts",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_TenantId",
                table: "AppCarts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId_Status",
                table: "AppCarts",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppCartItems");

            migrationBuilder.DropTable(
                name: "AppCarts");
        }
    }
}
