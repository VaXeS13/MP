using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MP.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeAndPayPalTransactionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "AppRentalItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppPaymentProviderConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "Identyfikator dostawcy płatności"),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Nazwa wyświetlana dostawcy"),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "Czy dostawca jest włączony"),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 100, comment: "Priorytet wyświetlania (niższy = wyższy priorytet)"),
                    ConfigurationData = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}", comment: "Dane konfiguracyjne w formacie JSON"),
                    SupportedCurrencies = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: "PLN", comment: "Obsługiwane waluty oddzielone przecinkiem"),
                    MinAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 1.00m, comment: "Minimalna kwota transakcji"),
                    MaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m, comment: "Maksymalna kwota transakcji (0 = brak limitu)"),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}", comment: "Dodatkowe metadane w formacie JSON"),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPaymentProviderConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppPayPalTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, comment: "PayPal Order ID"),
                    PayerId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "PayPal Payer ID"),
                    PaymentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "PayPal Payment ID (legacy)"),
                    CaptureId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "PayPal Capture ID"),
                    AuthorizationId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "PayPal Authorization ID"),
                    RefundId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "PayPal Refund ID"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Payment amount"),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "PLN", comment: "Currency code"),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, comment: "Payment description"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, comment: "Customer email"),
                    Environment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "sandbox", comment: "PayPal environment (sandbox/live)"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "CREATED", comment: "PayPal order status"),
                    FundingSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "PayPal funding source"),
                    ApprovalUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "PayPal approval URL"),
                    ReturnUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "Return URL after payment"),
                    CancelUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "Cancel URL if payment cancelled"),
                    ClientId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "PayPal Client ID used"),
                    PayPalMetadata = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true, comment: "PayPal-specific metadata JSON"),
                    WebhookId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "Webhook verification ID"),
                    PayPalFee = table.Column<decimal>(type: "decimal(18,2)", nullable: true, comment: "Fee charged by PayPal"),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true, comment: "Net amount after fees"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Payment completion date"),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Payment approval date"),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Payment capture date"),
                    LastStatusCheck = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Last status check date"),
                    StatusCheckCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Number of status checks performed"),
                    RentalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "Associated rental ID"),
                    CustomerDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Customer details from PayPal"),
                    DisputeId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "Dispute case ID if disputed"),
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
                    table.PrimaryKey("PK_AppPayPalTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppPayPalTransactions_AppRentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "AppRentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AppStripeTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentIntentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, comment: "Stripe PaymentIntent ID"),
                    ClientSecret = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true, comment: "PaymentIntent client secret"),
                    CustomerId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "Stripe Customer ID"),
                    PaymentMethodId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "Stripe Payment Method ID"),
                    SetupIntentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "Stripe SetupIntent ID"),
                    AmountCents = table.Column<long>(type: "bigint", nullable: false, comment: "Amount in cents (Stripe format)"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Amount in original currency"),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "pln", comment: "Currency code"),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, comment: "Payment description"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, comment: "Customer email"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "requires_payment_method", comment: "Stripe payment status"),
                    PaymentMethodType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "Payment method type (card, apple_pay, etc.)"),
                    ReturnUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "Return URL after payment"),
                    CancelUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "Cancel URL if payment cancelled"),
                    StripeMetadata = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true, comment: "Stripe-specific metadata JSON"),
                    WebhookSecret = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true, comment: "Webhook secret for verification"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Payment completion date"),
                    LastStatusCheck = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Last status check date"),
                    StatusCheckCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0, comment: "Number of status checks performed"),
                    RentalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "Associated rental ID"),
                    ChargeId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "Stripe Charge ID"),
                    StripeFee = table.Column<long>(type: "bigint", nullable: true, comment: "Fee charged by Stripe in cents"),
                    NetworkTransactionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, comment: "Network transaction ID"),
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
                    table.PrimaryKey("PK_AppStripeTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppStripeTransactions_AppRentals_RentalId",
                        column: x => x.RentalId,
                        principalTable: "AppRentals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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

            migrationBuilder.CreateIndex(
                name: "IX_PayPalTransactions_CaptureId",
                table: "AppPayPalTransactions",
                column: "CaptureId");

            migrationBuilder.CreateIndex(
                name: "IX_PayPalTransactions_CompletedAt",
                table: "AppPayPalTransactions",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PayPalTransactions_DisputeId",
                table: "AppPayPalTransactions",
                column: "DisputeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayPalTransactions_Email",
                table: "AppPayPalTransactions",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_PayPalTransactions_Environment",
                table: "AppPayPalTransactions",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_PayPalTransactions_FundingSource",
                table: "AppPayPalTransactions",
                column: "FundingSource");

            migrationBuilder.CreateIndex(
                name: "IX_PayPalTransactions_OrderId",
                table: "AppPayPalTransactions",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayPalTransactions_PayerId",
                table: "AppPayPalTransactions",
                column: "PayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PayPalTransactions_PaymentId",
                table: "AppPayPalTransactions",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayPalTransactions_RentalId",
                table: "AppPayPalTransactions",
                column: "RentalId");

            migrationBuilder.CreateIndex(
                name: "IX_PayPalTransactions_Status",
                table: "AppPayPalTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PayPalTransactions_StatusCheck",
                table: "AppPayPalTransactions",
                columns: new[] { "LastStatusCheck", "StatusCheckCount" });

            migrationBuilder.CreateIndex(
                name: "IX_StripeTransactions_ChargeId",
                table: "AppStripeTransactions",
                column: "ChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_StripeTransactions_CompletedAt",
                table: "AppStripeTransactions",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StripeTransactions_CustomerId",
                table: "AppStripeTransactions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_StripeTransactions_Email",
                table: "AppStripeTransactions",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_StripeTransactions_PaymentIntentId",
                table: "AppStripeTransactions",
                column: "PaymentIntentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StripeTransactions_RentalId",
                table: "AppStripeTransactions",
                column: "RentalId");

            migrationBuilder.CreateIndex(
                name: "IX_StripeTransactions_Status",
                table: "AppStripeTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StripeTransactions_StatusCheck",
                table: "AppStripeTransactions",
                columns: new[] { "LastStatusCheck", "StatusCheckCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppPaymentProviderConfigurations");

            migrationBuilder.DropTable(
                name: "AppPayPalTransactions");

            migrationBuilder.DropTable(
                name: "AppStripeTransactions");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "AppRentalItems");
        }
    }
}
