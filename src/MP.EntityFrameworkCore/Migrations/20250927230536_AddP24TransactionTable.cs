using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddP24TransactionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppP24Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, comment: "ID sesji płatności P24"),
                    MerchantId = table.Column<int>(type: "int", nullable: false, comment: "ID merchant P24"),
                    PosId = table.Column<int>(type: "int", nullable: false, comment: "ID POS P24"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Kwota płatności"),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "PLN", comment: "Waluta płatności"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, comment: "Email płatnika"),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, comment: "Opis płatności"),
                    Method = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "Metoda płatności"),
                    TransferLabel = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "Etykieta transferu"),
                    Sign = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false, comment: "Podpis CRC transakcji"),
                    OrderId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "ID zamówienia"),
                    Verified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Czy transakcja została zweryfikowana"),
                    ReturnUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "URL powrotu"),
                    Statement = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "Opis transakcji na wyciągu"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true, comment: "Dodatkowe właściwości JSON"),
                    ManualStatusCheckCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Liczba ręcznych sprawdzeń statusu"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "processing", comment: "Status płatności"),
                    LastStatusCheck = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Data ostatniego sprawdzenia statusu"),
                    RentalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "ID powiązanego wynajęcia"),
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
                    table.PrimaryKey("PK_AppP24Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppP24Transactions_AppRentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "AppRentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_P24Transactions_LastStatusCheck",
                table: "AppP24Transactions",
                column: "LastStatusCheck");

            migrationBuilder.CreateIndex(
                name: "IX_P24Transactions_RentalId",
                table: "AppP24Transactions",
                column: "RentalId");

            migrationBuilder.CreateIndex(
                name: "IX_P24Transactions_SessionId",
                table: "AppP24Transactions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_P24Transactions_Status",
                table: "AppP24Transactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_P24Transactions_StatusCheck",
                table: "AppP24Transactions",
                columns: new[] { "ManualStatusCheckCount", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppP24Transactions");
        }
    }
}
