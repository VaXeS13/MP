using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "AppTenantTerminalSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                comment: "Display name for this terminal configuration");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AppTenantTerminalSettings",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "Whether this is the active (default) terminal for checkout");

            migrationBuilder.CreateTable(
                name: "AppChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID nadawcy wiadomości"),
                    ReceiverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID odbiorcy wiadomości"),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false, comment: "Treść wiadomości"),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Czy wiadomość została przeczytana"),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Data przeczytania wiadomości"),
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
                    table.PrimaryKey("PK_AppChatMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSettlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID użytkownika"),
                    SettlementNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "Numer rozliczenia"),
                    Status = table.Column<int>(type: "int", nullable: false, comment: "Status rozliczenia"),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Całkowita kwota sprzedaży"),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Kwota prowizji"),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Kwota netto do wypłaty"),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "Notatki do rozliczenia"),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Numer konta bankowego"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Data przetworzenia rozliczenia"),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Data wypłaty"),
                    ProcessedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "ID osoby przetwarzającej rozliczenie"),
                    TransactionReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Referencja transakcji wypłaty"),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Powód odrzucenia rozliczenia"),
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
                    table.PrimaryKey("PK_AppSettlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppTenantFiscalPrinterSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "Fiscal printer provider identifier (e.g., posnet_thermal, elzab, novitus)"),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Display name for this fiscal printer configuration"),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Whether this fiscal printer configuration is enabled"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Whether this is the active (default) fiscal printer for checkout"),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false, defaultValue: "{}", comment: "Provider-specific configuration JSON"),
                    Region = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "PL", comment: "Region/country code"),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Company name for fiscal receipts"),
                    TaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "Tax identification number (NIP in Poland)"),
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
                    table.PrimaryKey("PK_AppTenantFiscalPrinterSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppUserNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID użytkownika"),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Typ powiadomienia"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Tytuł powiadomienia"),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false, comment: "Treść powiadomienia"),
                    Severity = table.Column<int>(type: "int", nullable: false, comment: "Poziom ważności powiadomienia"),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Czy powiadomienie zostało przeczytane"),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Data przeczytania powiadomienia"),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "URL akcji dla powiadomienia"),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Typ powiązanej encji"),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "ID powiązanej encji"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Data wygaśnięcia powiadomienia"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSettlementItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID rozliczenia"),
                    RentalItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "ID przedmiotu z wynajmu"),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Cena sprzedaży"),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Kwota prowizji"),
                    CustomerAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Kwota dla klienta"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettlementItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSettlementItems_AppRentalItems_RentalItemId",
                        column: x => x.RentalItemId,
                        principalTable: "AppRentalItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppSettlementItems_AppSettlements_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "AppSettlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantTerminalSettings_TenantId_IsActive",
                table: "AppTenantTerminalSettings",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_CreationTime",
                table: "AppChatMessages",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ReceiverId",
                table: "AppChatMessages",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ReceiverId_IsRead",
                table: "AppChatMessages",
                columns: new[] { "ReceiverId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId",
                table: "AppChatMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId_ReceiverId",
                table: "AppChatMessages",
                columns: new[] { "SenderId", "ReceiverId" });

            migrationBuilder.CreateIndex(
                name: "IX_SettlementItems_RentalItemId",
                table: "AppSettlementItems",
                column: "RentalItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementItems_SettlementId",
                table: "AppSettlementItems",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_CreationTime",
                table: "AppSettlements",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_ProcessedBy",
                table: "AppSettlements",
                column: "ProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_SettlementNumber",
                table: "AppSettlements",
                column: "SettlementNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_Status",
                table: "AppSettlements",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_UserId",
                table: "AppSettlements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_UserId_Status",
                table: "AppSettlements",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantFiscalPrinterSettings_ProviderId",
                table: "AppTenantFiscalPrinterSettings",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantFiscalPrinterSettings_TenantId",
                table: "AppTenantFiscalPrinterSettings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantFiscalPrinterSettings_TenantId_IsActive",
                table: "AppTenantFiscalPrinterSettings",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantFiscalPrinterSettings_TenantId_IsEnabled",
                table: "AppTenantFiscalPrinterSettings",
                columns: new[] { "TenantId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_CreationTime",
                table: "AppUserNotifications",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_ExpiresAt",
                table: "AppUserNotifications",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_RelatedEntity",
                table: "AppUserNotifications",
                columns: new[] { "RelatedEntityType", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_Type",
                table: "AppUserNotifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId",
                table: "AppUserNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_IsRead",
                table: "AppUserNotifications",
                columns: new[] { "UserId", "IsRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppChatMessages");

            migrationBuilder.DropTable(
                name: "AppSettlementItems");

            migrationBuilder.DropTable(
                name: "AppTenantFiscalPrinterSettings");

            migrationBuilder.DropTable(
                name: "AppUserNotifications");

            migrationBuilder.DropTable(
                name: "AppSettlements");

            migrationBuilder.DropIndex(
                name: "IX_TenantTerminalSettings_TenantId_IsActive",
                table: "AppTenantTerminalSettings");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "AppTenantTerminalSettings");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AppTenantTerminalSettings");
        }
    }
}
