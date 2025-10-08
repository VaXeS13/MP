using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddFloorPlanAndFloorPlanBooths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppFloorPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Nazwa planu piętra"),
                    Level = table.Column<int>(type: "int", nullable: false, comment: "Poziom piętra (0=parter, 1=pierwsze piętro, itd.)"),
                    Width = table.Column<int>(type: "int", nullable: false, comment: "Szerokość planu w pikselach"),
                    Height = table.Column<int>(type: "int", nullable: false, comment: "Wysokość planu w pikselach"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Czy plan jest aktywny/opublikowany"),
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
                    table.PrimaryKey("PK_AppFloorPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppFloorPlanBooths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FloorPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID planu piętra"),
                    BoothId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID stanowiska"),
                    X = table.Column<int>(type: "int", nullable: false, comment: "Pozycja X na planie"),
                    Y = table.Column<int>(type: "int", nullable: false, comment: "Pozycja Y na planie"),
                    Width = table.Column<int>(type: "int", nullable: false, comment: "Szerokość stanowiska na planie"),
                    Height = table.Column<int>(type: "int", nullable: false, comment: "Wysokość stanowiska na planie"),
                    Rotation = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Obrót stanowiska w stopniach (0-359)"),
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
                    table.PrimaryKey("PK_AppFloorPlanBooths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppFloorPlanBooths_AppBooths_BoothId",
                        column: x => x.BoothId,
                        principalTable: "AppBooths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppFloorPlanBooths_AppFloorPlans_FloorPlanId",
                        column: x => x.FloorPlanId,
                        principalTable: "AppFloorPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanBooths_BoothId",
                table: "AppFloorPlanBooths",
                column: "BoothId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanBooths_FloorPlanId",
                table: "AppFloorPlanBooths",
                column: "FloorPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanBooths_FloorPlanId_BoothId",
                table: "AppFloorPlanBooths",
                columns: new[] { "FloorPlanId", "BoothId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlans_IsActive",
                table: "AppFloorPlans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlans_TenantId_Level",
                table: "AppFloorPlans",
                columns: new[] { "TenantId", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlans_TenantId_Name",
                table: "AppFloorPlans",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppFloorPlanBooths");

            migrationBuilder.DropTable(
                name: "AppFloorPlans");
        }
    }
}
