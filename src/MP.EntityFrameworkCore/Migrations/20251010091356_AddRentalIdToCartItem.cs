using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddRentalIdToCartItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RentalId",
                table: "AppCartItems",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RentalId",
                table: "AppCartItems");
        }
    }
}
