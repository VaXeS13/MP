using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBoothTypeFromBooths_AddBoothTypeEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommissionPercentage",
                table: "AppBooths");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "AppBooths");

            migrationBuilder.AlterColumn<int>(
                name: "Currency",
                table: "AppBooths",
                type: "int",
                nullable: false,
                comment: "Waluta stanowiska",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "AppBoothTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Nazwa typu stanowiska"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, comment: "Opis typu stanowiska"),
                    CommissionPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false, comment: "Procent prowizji dla tego typu"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Czy typ jest aktywny"),
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
                    table.PrimaryKey("PK_AppBoothTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoothTypes_IsActive",
                table: "AppBoothTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BoothTypes_TenantId_Name",
                table: "AppBoothTypes",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppBoothTypes");

            migrationBuilder.AlterColumn<int>(
                name: "Currency",
                table: "AppBooths",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "Waluta stanowiska");

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionPercentage",
                table: "AppBooths",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Procent prowizji");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "AppBooths",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "Typ stanowiska (SelfPricing/ShopPricing)");
        }
    }
}
