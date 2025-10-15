using Microsoft.EntityFrameworkCore;
using MP.Domain;
using MP.Domain.Booths;
using MP.Domain.BoothTypes;
using MP.Domain.Rentals;
using MP.Domain.FloorPlans;
using MP.Domain.Payments;
using MP.Domain.Carts;
using MP.Domain.Terminals;
using MP.Domain.FiscalPrinters;
using MP.Domain.Notifications;
using MP.Domain.Settlements;
using MP.Domain.Chat;
using MP.Domain.Items;
using MP.Domain.Identity;
using MP.Domain.HomePageContent;
using MP.Domain.Files;
using MP.Carts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace MP.EntityFrameworkCore
{
    public static class MPDbContextModelCreatingExtensions
    {
        public static void ConfigureMP(this ModelBuilder builder)
        {
            Check.NotNull(builder, nameof(builder));

            // Explicitly ignore value objects that are used as owned types
            builder.Ignore<RentalPeriod>();
            builder.Ignore<Payment>();

            // Konfiguracja tabeli BoothTypes
            builder.Entity<BoothType>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "BoothTypes", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasComment("Nazwa typu stanowiska");

                b.Property(x => x.Description)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasComment("Opis typu stanowiska");

                b.Property(x => x.CommissionPercentage)
                    .IsRequired()
                    .HasColumnType("decimal(5,2)")
                    .HasComment("Procent prowizji dla tego typu");

                b.Property(x => x.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true)
                    .HasComment("Czy typ jest aktywny");

                // Indeks unikalności nazwy per tenant
                b.HasIndex(x => new { x.TenantId, x.Name })
                    .IsUnique()
                    .HasDatabaseName("IX_BoothTypes_TenantId_Name");

                b.HasIndex(x => x.IsActive)
                    .HasDatabaseName("IX_BoothTypes_IsActive");
            });

            // Konfiguracja tabeli Booths
            builder.Entity<Booth>(b =>
            {
                // Tabela
                b.ToTable(MPConsts.DbTablePrefix + "Booths", MPConsts.DbSchema);

                // Klucz główny
                b.ConfigureByConvention();

                // Właściwości
                b.Property(x => x.Number)
                    .IsRequired()
                    .HasMaxLength(10)
                    .HasComment("Numer stanowiska");

                b.Property(x => x.Status)
                    .IsRequired()
                    .HasComment("Status stanowiska");

                b.Property(x => x.PricePerDay)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Cena za dzień");

                // Indeksy
                // 🔄 Zmieniony indeks: unikalność per TenantId + Number
                b.HasIndex(x => new { x.TenantId, x.Number })
                    .IsUnique()
                    .HasDatabaseName("IX_Booths_TenantId_Number");

                b.HasIndex(x => x.Status)
                    .HasDatabaseName("IX_Booths_Status");
            });


            builder.Entity<Rental>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "Rentals", MPConsts.DbSchema);
                b.ConfigureByConvention();

                // Klucze obce
                b.Property(x => x.UserId).IsRequired();
                b.Property(x => x.BoothId).IsRequired();
                b.Property(x => x.BoothTypeId).IsRequired();
                b.Property(x => x.Status).IsRequired();

                b.Property(x => x.Currency)
                    .IsRequired()
                    .HasComment("Waluta wynajmu (snapshot at checkout)");

                // RentalPeriod jako owned type
                b.OwnsOne(x => x.Period, period =>
                {
                    period.Property(p => p.StartDate)
                          .IsRequired()
                          .HasColumnName("StartDate")
                          .HasColumnType("date");

                    period.Property(p => p.EndDate)
                          .IsRequired()
                          .HasColumnName("EndDate")
                          .HasColumnType("date");

                    // indeks na owned properties
                    period.HasIndex(p => new { p.StartDate, p.EndDate })
                          .HasDatabaseName("IX_Rentals_Period");
                });

                // Payment jako owned type
                b.OwnsOne(x => x.Payment, payment =>
                {
                    payment.Property(p => p.TotalAmount)
                           .IsRequired()
                           .HasColumnName("TotalAmount")
                           .HasColumnType("decimal(18,2)");

                    payment.Property(p => p.PaidAmount)
                           .IsRequired()
                           .HasColumnName("PaidAmount")
                           .HasColumnType("decimal(18,2)")
                           .HasDefaultValue(0);

                    payment.Property(p => p.PaidDate)
                           .HasColumnName("PaidDate")
                           .HasColumnType("datetime2");

                    payment.Property(p => p.PaymentStatus)
                           .IsRequired()
                           .HasColumnName("Payment_PaymentStatus")
                           .HasConversion<string>();

                    payment.Property(p => p.Przelewy24TransactionId)
                           .HasColumnName("Payment_Przelewy24TransactionId")
                           .HasMaxLength(100);

                    payment.Property(p => p.PaymentMethod)
                           .IsRequired()
                           .HasColumnName("Payment_PaymentMethod")
                           .HasDefaultValue(RentalPaymentMethod.Online);

                    payment.Property(p => p.TerminalTransactionId)
                           .HasColumnName("Payment_TerminalTransactionId")
                           .HasMaxLength(100);

                    payment.Property(p => p.TerminalReceiptNumber)
                           .HasColumnName("Payment_TerminalReceiptNumber")
                           .HasMaxLength(100);
                });

                // Właściwości opcjonalne
                b.Property(x => x.Notes).HasMaxLength(1000);
                b.Property(x => x.StartedAt).HasColumnType("datetime2");
                b.Property(x => x.CompletedAt).HasColumnType("datetime2");

                // Relacje
                b.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.Booth)
                    .WithMany()
                    .HasForeignKey(x => x.BoothId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(x => x.BoothType)
                    .WithMany()
                    .HasForeignKey(x => x.BoothTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Indeksy
                b.HasIndex(x => x.UserId);
                b.HasIndex(x => x.BoothId);
                b.HasIndex(x => x.BoothTypeId);
                b.HasIndex(x => x.Status);
                b.HasIndex(x => new { x.BoothId, x.Status });
            });

            // Konfiguracja tabeli RentalExtensionPayments
            builder.Entity<RentalExtensionPayment>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "RentalExtensionPayments", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.RentalId)
                    .IsRequired()
                    .HasComment("ID wynajmu");

                b.Property(x => x.OldEndDate)
                    .IsRequired()
                    .HasColumnType("date")
                    .HasComment("Poprzednia data zakończenia");

                b.Property(x => x.NewEndDate)
                    .IsRequired()
                    .HasColumnType("date")
                    .HasComment("Nowa data zakończenia");

                b.Property(x => x.ExtensionCost)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Koszt przedłużenia");

                b.Property(x => x.Currency)
                    .IsRequired()
                    .HasComment("Waluta przedłużenia (snapshot)");

                b.Property(x => x.PaymentType)
                    .IsRequired()
                    .HasComment("Typ płatności za przedłużenie");

                b.Property(x => x.ExtendedAt)
                    .IsRequired()
                    .HasColumnType("datetime2")
                    .HasComment("Data przedłużenia");

                b.Property(x => x.ExtendedBy)
                    .IsRequired()
                    .HasComment("ID użytkownika który wykonał przedłużenie");

                b.Property(x => x.TransactionId)
                    .HasMaxLength(100)
                    .HasComment("ID transakcji (dla Terminal/Online)");

                b.Property(x => x.ReceiptNumber)
                    .HasMaxLength(100)
                    .HasComment("Numer paragonu (dla Terminal)");

                // Relacja z Rental
                b.HasOne<Rental>()
                    .WithMany()
                    .HasForeignKey(x => x.RentalId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indeksy
                b.HasIndex(x => x.RentalId)
                    .HasDatabaseName("IX_RentalExtensionPayments_RentalId");

                b.HasIndex(x => x.ExtendedAt)
                    .HasDatabaseName("IX_RentalExtensionPayments_ExtendedAt");

                b.HasIndex(x => x.PaymentType)
                    .HasDatabaseName("IX_RentalExtensionPayments_PaymentType");

                b.HasIndex(x => x.ExtendedBy)
                    .HasDatabaseName("IX_RentalExtensionPayments_ExtendedBy");
            });

            // Konfiguracja RentalItems
            // RentalItem configuration removed - replaced by Item/ItemSheet system

            // Konfiguracja tabeli FloorPlans
            builder.Entity<FloorPlan>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "FloorPlans", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasComment("Nazwa planu piętra");

                b.Property(x => x.Level)
                    .IsRequired()
                    .HasComment("Poziom piętra (0=parter, 1=pierwsze piętro, itd.)");

                b.Property(x => x.Width)
                    .IsRequired()
                    .HasComment("Szerokość planu w pikselach");

                b.Property(x => x.Height)
                    .IsRequired()
                    .HasComment("Wysokość planu w pikselach");

                b.Property(x => x.IsActive)
                    .IsRequired()
                    .HasDefaultValue(false)
                    .HasComment("Czy plan jest aktywny/opublikowany");

                // Indeks unikalności nazwy per tenant
                b.HasIndex(x => new { x.TenantId, x.Name })
                    .IsUnique()
                    .HasDatabaseName("IX_FloorPlans_TenantId_Name");

                b.HasIndex(x => x.IsActive)
                    .HasDatabaseName("IX_FloorPlans_IsActive");

                b.HasIndex(x => new { x.TenantId, x.Level })
                    .HasDatabaseName("IX_FloorPlans_TenantId_Level");
            });

            // Konfiguracja tabeli FloorPlanBooths
            builder.Entity<FloorPlanBooth>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "FloorPlanBooths", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.FloorPlanId)
                    .IsRequired()
                    .HasComment("ID planu piętra");

                b.Property(x => x.BoothId)
                    .IsRequired()
                    .HasComment("ID stanowiska");

                b.Property(x => x.X)
                    .IsRequired()
                    .HasComment("Pozycja X na planie");

                b.Property(x => x.Y)
                    .IsRequired()
                    .HasComment("Pozycja Y na planie");

                b.Property(x => x.Width)
                    .IsRequired()
                    .HasComment("Szerokość stanowiska na planie");

                b.Property(x => x.Height)
                    .IsRequired()
                    .HasComment("Wysokość stanowiska na planie");

                b.Property(x => x.Rotation)
                    .IsRequired()
                    .HasDefaultValue(0)
                    .HasComment("Obrót stanowiska w stopniach (0-359)");

                // Relacje
                b.HasOne<FloorPlan>()
                    .WithMany()
                    .HasForeignKey(x => x.FloorPlanId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Booth)
                    .WithMany()
                    .HasForeignKey(x => x.BoothId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indeksy
                b.HasIndex(x => x.FloorPlanId)
                    .HasDatabaseName("IX_FloorPlanBooths_FloorPlanId");

                b.HasIndex(x => x.BoothId)
                    .HasDatabaseName("IX_FloorPlanBooths_BoothId");

                // Indeks unikalności booth per floor plan
                b.HasIndex(x => new { x.FloorPlanId, x.BoothId })
                    .IsUnique()
                    .HasDatabaseName("IX_FloorPlanBooths_FloorPlanId_BoothId");
            });

            // Konfiguracja tabeli FloorPlanElements
            builder.Entity<FloorPlanElement>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "FloorPlanElements", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.FloorPlanId)
                    .IsRequired()
                    .HasComment("ID planu piętra");

                b.Property(x => x.ElementType)
                    .IsRequired()
                    .HasComment("Typ elementu (ściana, drzwi, okno, etc.)");

                b.Property(x => x.X)
                    .IsRequired()
                    .HasComment("Pozycja X na planie");

                b.Property(x => x.Y)
                    .IsRequired()
                    .HasComment("Pozycja Y na planie");

                b.Property(x => x.Width)
                    .IsRequired()
                    .HasComment("Szerokość elementu na planie");

                b.Property(x => x.Height)
                    .IsRequired()
                    .HasComment("Wysokość elementu na planie");

                b.Property(x => x.Rotation)
                    .IsRequired()
                    .HasDefaultValue(0)
                    .HasComment("Obrót elementu w stopniach (0-359)");

                b.Property(x => x.Color)
                    .HasMaxLength(20)
                    .HasComment("Kolor elementu (hex lub nazwa)");

                b.Property(x => x.Text)
                    .HasMaxLength(500)
                    .HasComment("Tekst dla elementów typu TextLabel");

                b.Property(x => x.IconName)
                    .HasMaxLength(50)
                    .HasComment("Nazwa ikony dla danego elementu");

                b.Property(x => x.Thickness)
                    .HasComment("Grubość elementu (dla ścian, linii)");

                b.Property(x => x.Opacity)
                    .HasColumnType("decimal(3,2)")
                    .HasComment("Przezroczystość elementu (0.0 - 1.0)");

                b.Property(x => x.Direction)
                    .HasMaxLength(20)
                    .HasComment("Kierunek (dla drzwi: left, right, up, down)");

                // Relacje
                b.HasOne<FloorPlan>()
                    .WithMany()
                    .HasForeignKey(x => x.FloorPlanId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indeksy
                b.HasIndex(x => x.FloorPlanId)
                    .HasDatabaseName("IX_FloorPlanElements_FloorPlanId");

                b.HasIndex(x => x.ElementType)
                    .HasDatabaseName("IX_FloorPlanElements_ElementType");

                b.HasIndex(x => new { x.FloorPlanId, x.ElementType })
                    .HasDatabaseName("IX_FloorPlanElements_FloorPlanId_ElementType");
            });

            // Konfiguracja tabeli P24Transactions
            builder.Entity<P24Transaction>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "P24Transactions", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.SessionId)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("ID sesji płatności P24");

                b.Property(x => x.MerchantId)
                    .IsRequired()
                    .HasComment("ID merchant P24");

                b.Property(x => x.PosId)
                    .IsRequired()
                    .HasComment("ID POS P24");

                b.Property(x => x.Amount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Kwota płatności");

                b.Property(x => x.Currency)
                    .IsRequired()
                    .HasMaxLength(3)
                    .HasDefaultValue("PLN")
                    .HasComment("Waluta płatności");

                b.Property(x => x.Email)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("Email płatnika");

                b.Property(x => x.Description)
                    .IsRequired()
                    .HasMaxLength(1000)
                    .HasComment("Opis płatności");

                b.Property(x => x.Method)
                    .HasMaxLength(10)
                    .HasComment("Metoda płatności");

                b.Property(x => x.TransferLabel)
                    .HasMaxLength(255)
                    .HasComment("Etykieta transferu");

                b.Property(x => x.Sign)
                    .IsRequired()
                    .HasMaxLength(512)
                    .HasComment("Podpis CRC transakcji");

                b.Property(x => x.OrderId)
                    .HasMaxLength(255)
                    .HasComment("ID zamówienia");

                b.Property(x => x.Verified)
                    .IsRequired()
                    .HasDefaultValue(false)
                    .HasComment("Czy transakcja została zweryfikowana");

                b.Property(x => x.ReturnUrl)
                    .HasMaxLength(1000)
                    .HasComment("URL powrotu");

                b.Property(x => x.Statement)
                    .HasMaxLength(1000)
                    .HasComment("Opis transakcji na wyciągu");

                b.Property(x => x.ExtraProperties)
                    .HasMaxLength(2000)
                    .HasComment("Dodatkowe właściwości JSON");

                b.Property(x => x.ManualStatusCheckCount)
                    .IsRequired()
                    .HasDefaultValue(0)
                    .HasComment("Liczba ręcznych sprawdzeń statusu");

                b.Property(x => x.Status)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("processing")
                    .HasComment("Status płatności");

                b.Property(x => x.LastStatusCheck)
                    .HasColumnType("datetime2")
                    .HasComment("Data ostatniego sprawdzenia statusu");

                b.Property(x => x.RentalId)
                    .HasComment("ID powiązanego wynajęcia");

                // Indeksy
                b.HasIndex(x => x.SessionId)
                    .IsUnique()
                    .HasDatabaseName("IX_P24Transactions_SessionId");

                b.HasIndex(x => x.Status)
                    .HasDatabaseName("IX_P24Transactions_Status");

                b.HasIndex(x => x.RentalId)
                    .HasDatabaseName("IX_P24Transactions_RentalId");

                b.HasIndex(x => new { x.ManualStatusCheckCount, x.Status })
                    .HasDatabaseName("IX_P24Transactions_StatusCheck");

                b.HasIndex(x => x.LastStatusCheck)
                    .HasDatabaseName("IX_P24Transactions_LastStatusCheck");

                // Relacja z Rental (opcjonalna)
                b.HasOne<Rental>()
                    .WithMany()
                    .HasForeignKey(x => x.RentalId)
                    .OnDelete(DeleteBehavior.SetNull);
            });


            // Konfiguracja tabeli StripeTransactions
            builder.Entity<StripeTransaction>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "StripeTransactions", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.PaymentIntentId)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("Stripe PaymentIntent ID");

                b.Property(x => x.ClientSecret)
                    .HasMaxLength(512)
                    .HasComment("PaymentIntent client secret");

                b.Property(x => x.CustomerId)
                    .HasMaxLength(255)
                    .HasComment("Stripe Customer ID");

                b.Property(x => x.PaymentMethodId)
                    .HasMaxLength(255)
                    .HasComment("Stripe Payment Method ID");

                b.Property(x => x.SetupIntentId)
                    .HasMaxLength(255)
                    .HasComment("Stripe SetupIntent ID");

                b.Property(x => x.AmountCents)
                    .IsRequired()
                    .HasComment("Amount in cents (Stripe format)");

                b.Property(x => x.Amount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Amount in original currency");

                b.Property(x => x.Currency)
                    .IsRequired()
                    .HasMaxLength(3)
                    .HasDefaultValue("pln")
                    .HasComment("Currency code");

                b.Property(x => x.Description)
                    .IsRequired()
                    .HasMaxLength(1000)
                    .HasComment("Payment description");

                b.Property(x => x.Email)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("Customer email");

                b.Property(x => x.Status)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("requires_payment_method")
                    .HasComment("Stripe payment status");

                b.Property(x => x.PaymentMethodType)
                    .HasMaxLength(50)
                    .HasComment("Payment method type (card, apple_pay, etc.)");

                b.Property(x => x.ReturnUrl)
                    .HasMaxLength(1000)
                    .HasComment("Return URL after payment");

                b.Property(x => x.CancelUrl)
                    .HasMaxLength(1000)
                    .HasComment("Cancel URL if payment cancelled");

                b.Property(x => x.StripeMetadata)
                    .HasMaxLength(4000)
                    .HasComment("Stripe-specific metadata JSON");

                b.Property(x => x.WebhookSecret)
                    .HasMaxLength(512)
                    .HasComment("Webhook secret for verification");

                b.Property(x => x.CompletedAt)
                    .HasColumnType("datetime2")
                    .HasComment("Payment completion date");

                b.Property(x => x.LastStatusCheck)
                    .HasColumnType("datetime2")
                    .HasComment("Last status check date");

                b.Property(x => x.StatusCheckCount)
                    .IsRequired()
                    .HasDefaultValue(0)
                    .HasComment("Number of status checks performed");

                b.Property(x => x.RentalId)
                    .HasComment("Associated rental ID");

                b.Property(x => x.ChargeId)
                    .HasMaxLength(255)
                    .HasComment("Stripe Charge ID");

                b.Property(x => x.StripeFee)
                    .HasComment("Fee charged by Stripe in cents");

                b.Property(x => x.NetworkTransactionId)
                    .HasMaxLength(255)
                    .HasComment("Network transaction ID");

                // Indeksy
                b.HasIndex(x => x.PaymentIntentId)
                    .IsUnique()
                    .HasDatabaseName("IX_StripeTransactions_PaymentIntentId");

                b.HasIndex(x => x.Status)
                    .HasDatabaseName("IX_StripeTransactions_Status");

                b.HasIndex(x => x.RentalId)
                    .HasDatabaseName("IX_StripeTransactions_RentalId");

                b.HasIndex(x => x.Email)
                    .HasDatabaseName("IX_StripeTransactions_Email");

                b.HasIndex(x => x.CustomerId)
                    .HasDatabaseName("IX_StripeTransactions_CustomerId");

                b.HasIndex(x => x.ChargeId)
                    .HasDatabaseName("IX_StripeTransactions_ChargeId");

                b.HasIndex(x => new { x.LastStatusCheck, x.StatusCheckCount })
                    .HasDatabaseName("IX_StripeTransactions_StatusCheck");

                b.HasIndex(x => x.CompletedAt)
                    .HasDatabaseName("IX_StripeTransactions_CompletedAt");

                // Relacja z Rental (opcjonalna)
                b.HasOne<Rental>()
                    .WithMany()
                    .HasForeignKey(x => x.RentalId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Konfiguracja tabeli PayPalTransactions
            builder.Entity<PayPalTransaction>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "PayPalTransactions", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.OrderId)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("PayPal Order ID");

                b.Property(x => x.PayerId)
                    .HasMaxLength(255)
                    .HasComment("PayPal Payer ID");

                b.Property(x => x.PaymentId)
                    .HasMaxLength(255)
                    .HasComment("PayPal Payment ID (legacy)");

                b.Property(x => x.CaptureId)
                    .HasMaxLength(255)
                    .HasComment("PayPal Capture ID");

                b.Property(x => x.AuthorizationId)
                    .HasMaxLength(255)
                    .HasComment("PayPal Authorization ID");

                b.Property(x => x.RefundId)
                    .HasMaxLength(255)
                    .HasComment("PayPal Refund ID");

                b.Property(x => x.Amount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Payment amount");

                b.Property(x => x.Currency)
                    .IsRequired()
                    .HasMaxLength(3)
                    .HasDefaultValue("PLN")
                    .HasComment("Currency code");

                b.Property(x => x.Description)
                    .IsRequired()
                    .HasMaxLength(1000)
                    .HasComment("Payment description");

                b.Property(x => x.Email)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("Customer email");

                b.Property(x => x.Environment)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("sandbox")
                    .HasComment("PayPal environment (sandbox/live)");

                b.Property(x => x.Status)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("CREATED")
                    .HasComment("PayPal order status");

                b.Property(x => x.FundingSource)
                    .HasMaxLength(50)
                    .HasComment("PayPal funding source");

                b.Property(x => x.ApprovalUrl)
                    .HasMaxLength(1000)
                    .HasComment("PayPal approval URL");

                b.Property(x => x.ReturnUrl)
                    .HasMaxLength(1000)
                    .HasComment("Return URL after payment");

                b.Property(x => x.CancelUrl)
                    .HasMaxLength(1000)
                    .HasComment("Cancel URL if payment cancelled");

                b.Property(x => x.ClientId)
                    .HasMaxLength(255)
                    .HasComment("PayPal Client ID used");

                b.Property(x => x.PayPalMetadata)
                    .HasMaxLength(4000)
                    .HasComment("PayPal-specific metadata JSON");

                b.Property(x => x.WebhookId)
                    .HasMaxLength(255)
                    .HasComment("Webhook verification ID");

                b.Property(x => x.PayPalFee)
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Fee charged by PayPal");

                b.Property(x => x.NetAmount)
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Net amount after fees");

                b.Property(x => x.CompletedAt)
                    .HasColumnType("datetime2")
                    .HasComment("Payment completion date");

                b.Property(x => x.ApprovedAt)
                    .HasColumnType("datetime2")
                    .HasComment("Payment approval date");

                b.Property(x => x.CapturedAt)
                    .HasColumnType("datetime2")
                    .HasComment("Payment capture date");

                b.Property(x => x.LastStatusCheck)
                    .HasColumnType("datetime2")
                    .HasComment("Last status check date");

                b.Property(x => x.StatusCheckCount)
                    .IsRequired()
                    .HasDefaultValue(0)
                    .HasComment("Number of status checks performed");

                b.Property(x => x.RentalId)
                    .HasComment("Associated rental ID");

                b.Property(x => x.CustomerDetails)
                    .HasMaxLength(500)
                    .HasComment("Customer details from PayPal");

                b.Property(x => x.DisputeId)
                    .HasMaxLength(255)
                    .HasComment("Dispute case ID if disputed");

                // Indeksy
                b.HasIndex(x => x.OrderId)
                    .IsUnique()
                    .HasDatabaseName("IX_PayPalTransactions_OrderId");

                b.HasIndex(x => x.PaymentId)
                    .HasDatabaseName("IX_PayPalTransactions_PaymentId");

                b.HasIndex(x => x.CaptureId)
                    .HasDatabaseName("IX_PayPalTransactions_CaptureId");

                b.HasIndex(x => x.Status)
                    .HasDatabaseName("IX_PayPalTransactions_Status");

                b.HasIndex(x => x.RentalId)
                    .HasDatabaseName("IX_PayPalTransactions_RentalId");

                b.HasIndex(x => x.Email)
                    .HasDatabaseName("IX_PayPalTransactions_Email");

                b.HasIndex(x => x.PayerId)
                    .HasDatabaseName("IX_PayPalTransactions_PayerId");

                b.HasIndex(x => x.Environment)
                    .HasDatabaseName("IX_PayPalTransactions_Environment");

                b.HasIndex(x => x.FundingSource)
                    .HasDatabaseName("IX_PayPalTransactions_FundingSource");

                b.HasIndex(x => new { x.LastStatusCheck, x.StatusCheckCount })
                    .HasDatabaseName("IX_PayPalTransactions_StatusCheck");

                b.HasIndex(x => x.CompletedAt)
                    .HasDatabaseName("IX_PayPalTransactions_CompletedAt");

                b.HasIndex(x => x.DisputeId)
                    .HasDatabaseName("IX_PayPalTransactions_DisputeId");

                // Relacja z Rental (opcjonalna)
                b.HasOne<Rental>()
                    .WithMany()
                    .HasForeignKey(x => x.RentalId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Konfiguracja tabeli Carts
            builder.Entity<Cart>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "Carts", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.UserId)
                    .IsRequired()
                    .HasComment("ID użytkownika");

                b.Property(x => x.Status)
                    .IsRequired()
                    .HasComment("Status koszyka");

                b.Property(x => x.ExtensionTimeoutAt)
                    .HasColumnType("datetime2")
                    .HasComment("Czas wygaśnięcia koszyka przy przedłużeniu online");

                // Relacja z użytkownikiem
                b.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Indeksy
                b.HasIndex(x => new { x.UserId, x.Status })
                    .HasDatabaseName("IX_Carts_UserId_Status");

                b.HasIndex(x => x.TenantId)
                    .HasDatabaseName("IX_Carts_TenantId");

                b.HasIndex(x => x.CreationTime)
                    .HasDatabaseName("IX_Carts_CreationTime");

                b.HasIndex(x => x.ExtensionTimeoutAt)
                    .HasDatabaseName("IX_Carts_ExtensionTimeoutAt");
            });

            // Konfiguracja tabeli CartItems
            builder.Entity<CartItem>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "CartItems", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.CartId)
                    .IsRequired()
                    .HasComment("ID koszyka");

                b.Property(x => x.BoothId)
                    .IsRequired()
                    .HasComment("ID stanowiska");

                b.Property(x => x.BoothTypeId)
                    .IsRequired()
                    .HasComment("ID typu stanowiska");

                b.Property(x => x.StartDate)
                    .IsRequired()
                    .HasColumnType("date")
                    .HasComment("Data rozpoczęcia wynajmu");

                b.Property(x => x.EndDate)
                    .IsRequired()
                    .HasColumnType("date")
                    .HasComment("Data zakończenia wynajmu");

                b.Property(x => x.PricePerDay)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Cena za dzień");

                b.Property(x => x.Currency)
                    .IsRequired()
                    .HasComment("Waluta (snapshot from tenant)");

                b.Property(x => x.Notes)
                    .HasMaxLength(1000)
                    .HasComment("Notatki do wynajmu");

                b.Property(x => x.ItemType)
                    .IsRequired()
                    .HasDefaultValue(CartItemType.Rental)
                    .HasComment("Typ pozycji w koszyku (Rental/Extension)");

                b.Property(x => x.ExtendedRentalId)
                    .HasComment("ID wynajmu który jest przedłużany (dla ItemType=Extension)");

                // Relacja z Cart
                b.HasOne(x => x.Cart)
                    .WithMany(c => c.Items)
                    .HasForeignKey(x => x.CartId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relacja z przedłużanym Rental (opcjonalna)
                b.HasOne<Rental>()
                    .WithMany()
                    .HasForeignKey(x => x.ExtendedRentalId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Indeksy
                b.HasIndex(x => x.CartId)
                    .HasDatabaseName("IX_CartItems_CartId");

                b.HasIndex(x => x.BoothId)
                    .HasDatabaseName("IX_CartItems_BoothId");

                b.HasIndex(x => new { x.BoothId, x.StartDate, x.EndDate })
                    .HasDatabaseName("IX_CartItems_Booth_Period");

                b.HasIndex(x => x.ItemType)
                    .HasDatabaseName("IX_CartItems_ItemType");

                b.HasIndex(x => x.ExtendedRentalId)
                    .HasDatabaseName("IX_CartItems_ExtendedRentalId");
            });

            // Konfiguracja tabeli TenantTerminalSettings
            builder.Entity<TenantTerminalSettings>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "TenantTerminalSettings", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.ProviderId)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasComment("Terminal provider identifier (e.g., ingenico, verifone, stripe_terminal, mock)");

                b.Property(x => x.DisplayName)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasComment("Display name for this terminal configuration");

                b.Property(x => x.IsEnabled)
                    .IsRequired()
                    .HasDefaultValue(true)
                    .HasComment("Whether this terminal configuration is enabled");

                b.Property(x => x.IsActive)
                    .IsRequired()
                    .HasDefaultValue(false)
                    .HasComment("Whether this is the active (default) terminal for checkout");

                b.Property(x => x.ConfigurationJson)
                    .IsRequired()
                    .HasMaxLength(4000)
                    .HasDefaultValue("{}")
                    .HasComment("Provider-specific configuration JSON");

                b.Property(x => x.Currency)
                    .IsRequired()
                    .HasMaxLength(3)
                    .HasDefaultValue("PLN")
                    .HasComment("Currency code for terminal transactions");

                b.Property(x => x.Region)
                    .HasMaxLength(10)
                    .HasComment("Region/country code");

                b.Property(x => x.IsSandbox)
                    .IsRequired()
                    .HasDefaultValue(false)
                    .HasComment("Whether this is a sandbox/test configuration");

                // Indeksy
                b.HasIndex(x => x.TenantId)
                    .HasDatabaseName("IX_TenantTerminalSettings_TenantId");

                b.HasIndex(x => new { x.TenantId, x.IsEnabled })
                    .HasDatabaseName("IX_TenantTerminalSettings_TenantId_IsEnabled");

                b.HasIndex(x => x.ProviderId)
                    .HasDatabaseName("IX_TenantTerminalSettings_ProviderId");

                b.HasIndex(x => new { x.TenantId, x.IsActive })
                    .HasDatabaseName("IX_TenantTerminalSettings_TenantId_IsActive");
            });

            // Konfiguracja tabeli TenantFiscalPrinterSettings
            builder.Entity<TenantFiscalPrinterSettings>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "TenantFiscalPrinterSettings", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.ProviderId)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasComment("Fiscal printer provider identifier (e.g., posnet_thermal, elzab, novitus)");

                b.Property(x => x.DisplayName)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasComment("Display name for this fiscal printer configuration");

                b.Property(x => x.IsEnabled)
                    .IsRequired()
                    .HasDefaultValue(true)
                    .HasComment("Whether this fiscal printer configuration is enabled");

                b.Property(x => x.IsActive)
                    .IsRequired()
                    .HasDefaultValue(false)
                    .HasComment("Whether this is the active (default) fiscal printer for checkout");

                b.Property(x => x.ConfigurationJson)
                    .IsRequired()
                    .HasMaxLength(4000)
                    .HasDefaultValue("{}")
                    .HasComment("Provider-specific configuration JSON");

                b.Property(x => x.Region)
                    .IsRequired()
                    .HasMaxLength(10)
                    .HasDefaultValue("PL")
                    .HasComment("Region/country code");

                b.Property(x => x.CompanyName)
                    .HasMaxLength(200)
                    .HasComment("Company name for fiscal receipts");

                b.Property(x => x.TaxId)
                    .HasMaxLength(50)
                    .HasComment("Tax identification number (NIP in Poland)");

                // Indeksy
                b.HasIndex(x => x.TenantId)
                    .HasDatabaseName("IX_TenantFiscalPrinterSettings_TenantId");

                b.HasIndex(x => new { x.TenantId, x.IsEnabled })
                    .HasDatabaseName("IX_TenantFiscalPrinterSettings_TenantId_IsEnabled");

                b.HasIndex(x => new { x.TenantId, x.IsActive })
                    .HasDatabaseName("IX_TenantFiscalPrinterSettings_TenantId_IsActive");

                b.HasIndex(x => x.ProviderId)
                    .HasDatabaseName("IX_TenantFiscalPrinterSettings_ProviderId");
            });

            // Konfiguracja tabeli UserNotifications
            builder.Entity<UserNotification>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "UserNotifications", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.UserId)
                    .IsRequired()
                    .HasComment("ID użytkownika");

                b.Property(x => x.Type)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasComment("Typ powiadomienia");

                b.Property(x => x.Title)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasComment("Tytuł powiadomienia");

                b.Property(x => x.Message)
                    .IsRequired()
                    .HasMaxLength(2000)
                    .HasComment("Treść powiadomienia");

                b.Property(x => x.Severity)
                    .IsRequired()
                    .HasComment("Poziom ważności powiadomienia");

                b.Property(x => x.IsRead)
                    .IsRequired()
                    .HasDefaultValue(false)
                    .HasComment("Czy powiadomienie zostało przeczytane");

                b.Property(x => x.ReadAt)
                    .HasColumnType("datetime2")
                    .HasComment("Data przeczytania powiadomienia");

                b.Property(x => x.ActionUrl)
                    .HasMaxLength(500)
                    .HasComment("URL akcji dla powiadomienia");

                b.Property(x => x.RelatedEntityType)
                    .HasMaxLength(100)
                    .HasComment("Typ powiązanej encji");

                b.Property(x => x.RelatedEntityId)
                    .HasComment("ID powiązanej encji");

                b.Property(x => x.ExpiresAt)
                    .HasColumnType("datetime2")
                    .HasComment("Data wygaśnięcia powiadomienia");

                // Indeksy
                b.HasIndex(x => x.UserId)
                    .HasDatabaseName("IX_UserNotifications_UserId");

                b.HasIndex(x => new { x.UserId, x.IsRead })
                    .HasDatabaseName("IX_UserNotifications_UserId_IsRead");

                b.HasIndex(x => x.Type)
                    .HasDatabaseName("IX_UserNotifications_Type");

                b.HasIndex(x => x.ExpiresAt)
                    .HasDatabaseName("IX_UserNotifications_ExpiresAt");

                b.HasIndex(x => x.CreationTime)
                    .HasDatabaseName("IX_UserNotifications_CreationTime");

                b.HasIndex(x => new { x.RelatedEntityType, x.RelatedEntityId })
                    .HasDatabaseName("IX_UserNotifications_RelatedEntity");
            });

            // Konfiguracja tabeli Settlements
            builder.Entity<Settlement>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "Settlements", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.UserId)
                    .IsRequired()
                    .HasComment("ID użytkownika");

                b.Property(x => x.SettlementNumber)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasComment("Numer rozliczenia");

                b.Property(x => x.Status)
                    .IsRequired()
                    .HasComment("Status rozliczenia");

                b.Property(x => x.TotalAmount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Całkowita kwota sprzedaży");

                b.Property(x => x.CommissionAmount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Kwota prowizji");

                b.Property(x => x.NetAmount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Kwota netto do wypłaty");

                b.Property(x => x.Notes)
                    .HasMaxLength(1000)
                    .HasComment("Notatki do rozliczenia");

                b.Property(x => x.BankAccountNumber)
                    .HasMaxLength(100)
                    .HasComment("Numer konta bankowego");

                b.Property(x => x.ProcessedAt)
                    .HasColumnType("datetime2")
                    .HasComment("Data przetworzenia rozliczenia");

                b.Property(x => x.PaidAt)
                    .HasColumnType("datetime2")
                    .HasComment("Data wypłaty");

                b.Property(x => x.ProcessedBy)
                    .HasComment("ID osoby przetwarzającej rozliczenie");

                b.Property(x => x.TransactionReference)
                    .HasMaxLength(200)
                    .HasComment("Referencja transakcji wypłaty");

                b.Property(x => x.RejectionReason)
                    .HasMaxLength(500)
                    .HasComment("Powód odrzucenia rozliczenia");

                // Indeksy
                b.HasIndex(x => x.UserId)
                    .HasDatabaseName("IX_Settlements_UserId");

                b.HasIndex(x => x.SettlementNumber)
                    .IsUnique()
                    .HasDatabaseName("IX_Settlements_SettlementNumber");

                b.HasIndex(x => x.Status)
                    .HasDatabaseName("IX_Settlements_Status");

                b.HasIndex(x => new { x.UserId, x.Status })
                    .HasDatabaseName("IX_Settlements_UserId_Status");

                b.HasIndex(x => x.CreationTime)
                    .HasDatabaseName("IX_Settlements_CreationTime");

                b.HasIndex(x => x.ProcessedBy)
                    .HasDatabaseName("IX_Settlements_ProcessedBy");
            });

            // Konfiguracja tabeli SettlementItems
            builder.Entity<SettlementItem>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "SettlementItems", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.SettlementId)
                    .IsRequired()
                    .HasComment("ID rozliczenia");

                b.Property(x => x.ItemSheetItemId)
                    .IsRequired()
                    .HasComment("ID przedmiotu z arkusza");

                b.Property(x => x.SalePrice)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Cena sprzedaży");

                b.Property(x => x.CommissionAmount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Kwota prowizji");

                b.Property(x => x.CustomerAmount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Kwota dla klienta");

                // Relacja z Settlement
                b.HasOne<Settlement>()
                    .WithMany(s => s.Items)
                    .HasForeignKey(x => x.SettlementId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relacja z ItemSheetItem
                b.HasOne(x => x.ItemSheetItem)
                    .WithMany()
                    .HasForeignKey(x => x.ItemSheetItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Indeksy
                b.HasIndex(x => x.SettlementId)
                    .HasDatabaseName("IX_SettlementItems_SettlementId");

                b.HasIndex(x => x.ItemSheetItemId)
                    .HasDatabaseName("IX_SettlementItems_ItemSheetItemId");
            });

            // Konfiguracja tabeli ChatMessages
            builder.Entity<ChatMessage>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "ChatMessages", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.SenderId)
                    .IsRequired()
                    .HasComment("ID nadawcy wiadomości");

                b.Property(x => x.ReceiverId)
                    .IsRequired()
                    .HasComment("ID odbiorcy wiadomości");

                b.Property(x => x.Message)
                    .IsRequired()
                    .HasMaxLength(4000)
                    .HasComment("Treść wiadomości");

                b.Property(x => x.IsRead)
                    .IsRequired()
                    .HasDefaultValue(false)
                    .HasComment("Czy wiadomość została przeczytana");

                b.Property(x => x.ReadAt)
                    .HasColumnType("datetime2")
                    .HasComment("Data przeczytania wiadomości");

                // Indeksy
                b.HasIndex(x => x.SenderId)
                    .HasDatabaseName("IX_ChatMessages_SenderId");

                b.HasIndex(x => x.ReceiverId)
                    .HasDatabaseName("IX_ChatMessages_ReceiverId");

                b.HasIndex(x => new { x.SenderId, x.ReceiverId })
                    .HasDatabaseName("IX_ChatMessages_SenderId_ReceiverId");

                b.HasIndex(x => new { x.ReceiverId, x.IsRead })
                    .HasDatabaseName("IX_ChatMessages_ReceiverId_IsRead");

                b.HasIndex(x => x.CreationTime)
                    .HasDatabaseName("IX_ChatMessages_CreationTime");
            });

            // Konfiguracja tabeli Items
            builder.Entity<Item>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "Items", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.UserId)
                    .IsRequired()
                    .HasComment("ID użytkownika - właściciela przedmiotu");

                b.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasComment("Nazwa przedmiotu");

                b.Property(x => x.Category)
                    .HasMaxLength(100)
                    .HasComment("Kategoria przedmiotu");

                b.Property(x => x.Price)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Cena przedmiotu");

                b.Property(x => x.Currency)
                    .IsRequired()
                    .HasComment("Waluta ceny");

                b.Property(x => x.Status)
                    .IsRequired()
                    .HasComment("Status przedmiotu");

                // Relacja z użytkownikiem
                b.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Indeksy
                b.HasIndex(x => x.UserId)
                    .HasDatabaseName("IX_Items_UserId");

                b.HasIndex(x => new { x.UserId, x.Status })
                    .HasDatabaseName("IX_Items_UserId_Status");

                b.HasIndex(x => x.Category)
                    .HasDatabaseName("IX_Items_Category");

                b.HasIndex(x => x.Status)
                    .HasDatabaseName("IX_Items_Status");
            });

            // Konfiguracja tabeli ItemSheets
            builder.Entity<ItemSheet>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "ItemSheets", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.UserId)
                    .IsRequired()
                    .HasComment("ID użytkownika - właściciela arkusza");

                b.Property(x => x.RentalId)
                    .HasComment("ID wynajmu (nullable - może być nieprzypisany)");

                b.Property(x => x.Status)
                    .IsRequired()
                    .HasComment("Status arkusza");

                // Relacja z użytkownikiem
                b.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relacja z Rental (opcjonalna)
                b.HasOne(x => x.Rental)
                    .WithMany(r => r.ItemSheets)
                    .HasForeignKey(x => x.RentalId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indeksy
                b.HasIndex(x => x.UserId)
                    .HasDatabaseName("IX_ItemSheets_UserId");

                b.HasIndex(x => x.RentalId)
                    .HasDatabaseName("IX_ItemSheets_RentalId");

                b.HasIndex(x => new { x.UserId, x.Status })
                    .HasDatabaseName("IX_ItemSheets_UserId_Status");

                b.HasIndex(x => x.Status)
                    .HasDatabaseName("IX_ItemSheets_Status");
            });

            // Konfiguracja tabeli ItemSheetItems
            builder.Entity<ItemSheetItem>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "ItemSheetItems", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.ItemSheetId)
                    .IsRequired()
                    .HasComment("ID arkusza");

                b.Property(x => x.ItemId)
                    .IsRequired()
                    .HasComment("ID przedmiotu");

                b.Property(x => x.ItemNumber)
                    .IsRequired()
                    .HasComment("Numer pozycji w arkuszu");

                b.Property(x => x.Barcode)
                    .HasMaxLength(20)
                    .HasComment("Barcode (Base36 z Guid)");

                b.Property(x => x.CommissionPercentage)
                    .IsRequired()
                    .HasColumnType("decimal(5,2)")
                    .HasComment("Procent prowizji");

                b.Property(x => x.Status)
                    .IsRequired()
                    .HasComment("Status pozycji");

                b.Property(x => x.SoldAt)
                    .HasColumnType("datetime2")
                    .HasComment("Data sprzedaży");

                // Relacja z ItemSheet
                b.HasOne(x => x.ItemSheet)
                    .WithMany(s => s.Items)
                    .HasForeignKey(x => x.ItemSheetId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relacja z Item
                b.HasOne(x => x.Item)
                    .WithMany()
                    .HasForeignKey(x => x.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Indeksy
                b.HasIndex(x => x.ItemSheetId)
                    .HasDatabaseName("IX_ItemSheetItems_ItemSheetId");

                b.HasIndex(x => x.ItemId)
                    .HasDatabaseName("IX_ItemSheetItems_ItemId");

                b.HasIndex(x => x.Barcode)
                    .IsUnique()
                    .HasDatabaseName("IX_ItemSheetItems_Barcode");

                b.HasIndex(x => x.Status)
                    .HasDatabaseName("IX_ItemSheetItems_Status");

                b.HasIndex(x => new { x.ItemSheetId, x.ItemNumber })
                    .IsUnique()
                    .HasDatabaseName("IX_ItemSheetItems_ItemSheetId_ItemNumber");
            });

            // Konfiguracja tabeli Promotions
            builder.Entity<MP.Domain.Promotions.Promotion>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "Promotions", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasComment("Promotion name");

                b.Property(x => x.Description)
                    .HasMaxLength(1000)
                    .HasComment("Promotion description");

                b.Property(x => x.Type)
                    .IsRequired()
                    .HasComment("Promotion type (Quantity, PromoCode, DateRange)");

                b.Property(x => x.DisplayMode)
                    .IsRequired()
                    .HasComment("Display mode for customer notification");

                b.Property(x => x.IsActive)
                    .IsRequired()
                    .HasDefaultValue(false)
                    .HasComment("Whether promotion is active");

                b.Property(x => x.ValidFrom)
                    .HasColumnType("datetime2")
                    .HasComment("Promotion start date");

                b.Property(x => x.ValidTo)
                    .HasColumnType("datetime2")
                    .HasComment("Promotion end date");

                b.Property(x => x.Priority)
                    .IsRequired()
                    .HasDefaultValue(0)
                    .HasComment("Priority for display (higher = shown first)");

                b.Property(x => x.MinimumBoothsCount)
                    .HasComment("Minimum booths required");

                b.Property(x => x.PromoCode)
                    .HasMaxLength(50)
                    .HasComment("Promo code");

                b.Property(x => x.RequiresPromoCode)
                    .IsRequired()
                    .HasDefaultValue(false)
                    .HasComment("Whether promo code is required");

                b.Property(x => x.DiscountType)
                    .IsRequired()
                    .HasComment("Discount type (Percentage or FixedAmount)");

                b.Property(x => x.DiscountValue)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Discount value");

                b.Property(x => x.MaxDiscountAmount)
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Max discount amount (for percentage)");

                b.Property(x => x.MaxUsageCount)
                    .HasComment("Maximum total uses");

                b.Property(x => x.CurrentUsageCount)
                    .IsRequired()
                    .HasDefaultValue(0)
                    .HasComment("Current usage count");

                b.Property(x => x.MaxUsagePerUser)
                    .HasComment("Maximum uses per user");

                b.Property(x => x.CustomerMessage)
                    .HasMaxLength(500)
                    .HasComment("Customer message");

                // Indeksy
                b.HasIndex(x => x.TenantId)
                    .HasDatabaseName("IX_Promotions_TenantId");

                b.HasIndex(x => x.IsActive)
                    .HasDatabaseName("IX_Promotions_IsActive");

                b.HasIndex(x => x.PromoCode)
                    .HasDatabaseName("IX_Promotions_PromoCode");

                b.HasIndex(x => new { x.TenantId, x.PromoCode })
                    .HasDatabaseName("IX_Promotions_TenantId_PromoCode");

                b.HasIndex(x => new { x.IsActive, x.ValidFrom, x.ValidTo })
                    .HasDatabaseName("IX_Promotions_Active_Validity");

                b.HasIndex(x => x.Priority)
                    .HasDatabaseName("IX_Promotions_Priority");

                b.HasIndex(x => x.Type)
                    .HasDatabaseName("IX_Promotions_Type");
            });

            // Konfiguracja tabeli PromotionUsages
            builder.Entity<MP.Domain.Promotions.PromotionUsage>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "PromotionUsages", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.PromotionId)
                    .IsRequired()
                    .HasComment("Promotion ID");

                b.Property(x => x.UserId)
                    .IsRequired()
                    .HasComment("User ID");

                b.Property(x => x.CartId)
                    .IsRequired()
                    .HasComment("Cart ID");

                b.Property(x => x.RentalId)
                    .HasComment("Rental ID (optional)");

                b.Property(x => x.DiscountAmount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Discount amount applied");

                b.Property(x => x.PromoCodeUsed)
                    .HasMaxLength(50)
                    .HasComment("Promo code used");

                b.Property(x => x.OriginalAmount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Original cart amount");

                b.Property(x => x.FinalAmount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)")
                    .HasComment("Final cart amount after discount");

                // Relacje
                b.HasOne(x => x.Promotion)
                    .WithMany()
                    .HasForeignKey(x => x.PromotionId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne<MP.Domain.Carts.Cart>()
                    .WithMany()
                    .HasForeignKey(x => x.CartId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne<Rental>()
                    .WithMany()
                    .HasForeignKey(x => x.RentalId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indeksy
                b.HasIndex(x => x.PromotionId)
                    .HasDatabaseName("IX_PromotionUsages_PromotionId");

                b.HasIndex(x => x.UserId)
                    .HasDatabaseName("IX_PromotionUsages_UserId");

                b.HasIndex(x => x.CartId)
                    .HasDatabaseName("IX_PromotionUsages_CartId");

                b.HasIndex(x => x.RentalId)
                    .HasDatabaseName("IX_PromotionUsages_RentalId");

                b.HasIndex(x => new { x.PromotionId, x.UserId })
                    .HasDatabaseName("IX_PromotionUsages_PromotionId_UserId");

                b.HasIndex(x => x.CreationTime)
                    .HasDatabaseName("IX_PromotionUsages_CreationTime");
            });

            // Configure UserProfile
            builder.Entity<UserProfile>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "UserProfiles", MPConsts.DbSchema);
                b.ConfigureByConvention();

                // Configure UserId as primary key and foreign key
                b.HasKey(x => x.UserId);

                // Configure the BankAccountNumber property
                b.Property(x => x.BankAccountNumber)
                    .HasMaxLength(50)
                    .IsRequired(false);

                // Configure relationship with IdentityUser
                b.HasOne(x => x.User)
                    .WithOne() // IdentityUser doesn't have navigation back to UserProfile
                    .HasForeignKey<UserProfile>(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Konfiguracja tabeli HomePageSections
            builder.Entity<HomePageSection>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "HomePageSections", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.SectionType)
                    .IsRequired()
                    .HasComment("Type of homepage section");

                b.Property(x => x.Title)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasComment("Section title");

                b.Property(x => x.Subtitle)
                    .HasMaxLength(500)
                    .HasComment("Section subtitle");

                b.Property(x => x.Content)
                    .HasMaxLength(10000)
                    .HasComment("HTML content for the section");

                b.Property(x => x.ImageFileId)
                    .HasComment("ID of the uploaded image file");

                b.Property(x => x.LinkUrl)
                    .HasMaxLength(2000)
                    .HasComment("URL for CTA link");

                b.Property(x => x.LinkText)
                    .HasMaxLength(100)
                    .HasComment("Text for CTA button/link");

                b.Property(x => x.Order)
                    .IsRequired()
                    .HasComment("Display order (lower = displayed first)");

                b.Property(x => x.IsActive)
                    .IsRequired()
                    .HasDefaultValue(false)
                    .HasComment("Whether the section is active/published");

                b.Property(x => x.ValidFrom)
                    .HasColumnType("datetime2")
                    .HasComment("Start date for scheduled publishing");

                b.Property(x => x.ValidTo)
                    .HasColumnType("datetime2")
                    .HasComment("End date for scheduled publishing");

                b.Property(x => x.BackgroundColor)
                    .HasMaxLength(50)
                    .HasComment("Background color (hex code)");

                b.Property(x => x.TextColor)
                    .HasMaxLength(50)
                    .HasComment("Text color (hex code)");

                // Indeksy
                b.HasIndex(x => x.TenantId)
                    .HasDatabaseName("IX_HomePageSections_TenantId");

                b.HasIndex(x => x.IsActive)
                    .HasDatabaseName("IX_HomePageSections_IsActive");

                b.HasIndex(x => x.Order)
                    .HasDatabaseName("IX_HomePageSections_Order");

                b.HasIndex(x => new { x.TenantId, x.IsActive, x.Order })
                    .HasDatabaseName("IX_HomePageSections_TenantId_IsActive_Order");

                b.HasIndex(x => x.SectionType)
                    .HasDatabaseName("IX_HomePageSections_SectionType");

                b.HasIndex(x => new { x.IsActive, x.ValidFrom, x.ValidTo })
                    .HasDatabaseName("IX_HomePageSections_Active_Validity");

                b.HasIndex(x => x.ImageFileId)
                    .HasDatabaseName("IX_HomePageSections_ImageFileId");

                // Relacja z UploadedFile (opcjonalna)
                b.HasOne<UploadedFile>()
                    .WithMany()
                    .HasForeignKey(x => x.ImageFileId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Konfiguracja tabeli UploadedFiles
            builder.Entity<UploadedFile>(b =>
            {
                b.ToTable(MPConsts.DbTablePrefix + "UploadedFiles", MPConsts.DbSchema);
                b.ConfigureByConvention();

                b.Property(x => x.FileName)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("Original filename");

                b.Property(x => x.ContentType)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasComment("MIME type of the file");

                b.Property(x => x.FileSize)
                    .IsRequired()
                    .HasComment("File size in bytes");

                b.Property(x => x.Content)
                    .IsRequired()
                    .HasComment("Binary content of the file");

                b.Property(x => x.Description)
                    .HasMaxLength(500)
                    .HasComment("Optional description");

                // Indeksy
                b.HasIndex(x => x.TenantId)
                    .HasDatabaseName("IX_UploadedFiles_TenantId");

                b.HasIndex(x => x.ContentType)
                    .HasDatabaseName("IX_UploadedFiles_ContentType");

                b.HasIndex(x => x.CreationTime)
                    .HasDatabaseName("IX_UploadedFiles_CreationTime");
            });
        }
    }
}