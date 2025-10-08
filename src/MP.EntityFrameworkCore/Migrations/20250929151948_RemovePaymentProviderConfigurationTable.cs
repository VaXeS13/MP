using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class RemovePaymentProviderConfigurationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppPaymentProviderConfigurations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppPaymentProviderConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ConfigurationData = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}", comment: "Dane konfiguracyjne w formacie JSON"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Nazwa wyświetlana dostawcy"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Czy dostawca jest włączony"),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m, comment: "Maksymalna kwota transakcji (0 = brak limitu)"),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}", comment: "Dodatkowe metadane w formacie JSON"),
                    MinAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 1.00m, comment: "Minimalna kwota transakcji"),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 100, comment: "Priorytet wyświetlania (niższy = wyższy priorytet)"),
                    ProviderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "Identyfikator dostawcy płatności"),
                    SupportedCurrencies = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: "PLN", comment: "Obsługiwane waluty oddzielone przecinkiem"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPaymentProviderConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderConfigurations_IsEnabled",
                table: "AppPaymentProviderConfigurations",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderConfigurations_TenantId_IsEnabled_Priority",
                table: "AppPaymentProviderConfigurations",
                columns: new[] { "TenantId", "IsEnabled", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderConfigurations_TenantId_ProviderId",
                table: "AppPaymentProviderConfigurations",
                columns: new[] { "TenantId", "ProviderId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }
    }
}
