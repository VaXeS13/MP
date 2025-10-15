using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadedFilesAndUpdateHomePageSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "AppHomePageSections");

            migrationBuilder.AddColumn<Guid>(
                name: "ImageFileId",
                table: "AppHomePageSections",
                type: "uniqueidentifier",
                nullable: true,
                comment: "ID of the uploaded image file");

            migrationBuilder.CreateTable(
                name: "AppUploadedFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, comment: "Original filename"),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "MIME type of the file"),
                    FileSize = table.Column<long>(type: "bigint", nullable: false, comment: "File size in bytes"),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false, comment: "Binary content of the file"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Optional description"),
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
                    table.PrimaryKey("PK_AppUploadedFiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomePageSections_ImageFileId",
                table: "AppHomePageSections",
                column: "ImageFileId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_ContentType",
                table: "AppUploadedFiles",
                column: "ContentType");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_CreationTime",
                table: "AppUploadedFiles",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_TenantId",
                table: "AppUploadedFiles",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppHomePageSections_AppUploadedFiles_ImageFileId",
                table: "AppHomePageSections",
                column: "ImageFileId",
                principalTable: "AppUploadedFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppHomePageSections_AppUploadedFiles_ImageFileId",
                table: "AppHomePageSections");

            migrationBuilder.DropTable(
                name: "AppUploadedFiles");

            migrationBuilder.DropIndex(
                name: "IX_HomePageSections_ImageFileId",
                table: "AppHomePageSections");

            migrationBuilder.DropColumn(
                name: "ImageFileId",
                table: "AppHomePageSections");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "AppHomePageSections",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                comment: "URL to the main image");
        }
    }
}
