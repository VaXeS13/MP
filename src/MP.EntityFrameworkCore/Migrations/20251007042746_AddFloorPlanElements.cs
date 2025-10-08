using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddFloorPlanElements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppFloorPlanElements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FloorPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID planu piętra"),
                    ElementType = table.Column<int>(type: "int", nullable: false, comment: "Typ elementu (ściana, drzwi, okno, etc.)"),
                    X = table.Column<int>(type: "int", nullable: false, comment: "Pozycja X na planie"),
                    Y = table.Column<int>(type: "int", nullable: false, comment: "Pozycja Y na planie"),
                    Width = table.Column<int>(type: "int", nullable: false, comment: "Szerokość elementu na planie"),
                    Height = table.Column<int>(type: "int", nullable: false, comment: "Wysokość elementu na planie"),
                    Rotation = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Obrót elementu w stopniach (0-359)"),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "Kolor elementu (hex lub nazwa)"),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Tekst dla elementów typu TextLabel"),
                    IconName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "Nazwa ikony dla danego elementu"),
                    Thickness = table.Column<int>(type: "int", nullable: true, comment: "Grubość elementu (dla ścian, linii)"),
                    Opacity = table.Column<decimal>(type: "decimal(3,2)", nullable: true, comment: "Przezroczystość elementu (0.0 - 1.0)"),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "Kierunek (dla drzwi: left, right, up, down)"),
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
                    table.PrimaryKey("PK_AppFloorPlanElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppFloorPlanElements_AppFloorPlans_FloorPlanId",
                        column: x => x.FloorPlanId,
                        principalTable: "AppFloorPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanElements_ElementType",
                table: "AppFloorPlanElements",
                column: "ElementType");

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanElements_FloorPlanId",
                table: "AppFloorPlanElements",
                column: "FloorPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanElements_FloorPlanId_ElementType",
                table: "AppFloorPlanElements",
                columns: new[] { "FloorPlanId", "ElementType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppFloorPlanElements");
        }
    }
}
