using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Settings;
using Stripe;
using MP.Domain.Settings;
using MP.Domain.Payments;
using MP.Domain.Rentals;
using MP.Domain.Booths;
using MP.Domain.Promotions;
using MP.Domain.Carts;

namespace MP.Application.Payments
{
    /// <summary>
    /// Handles Stripe webhook events for payment notifications
    /// </summary>
    public class StripeWebhookHandler : ITransientDependency
    {
        private readonly ISettingProvider _settingProvider;
        private readonly IStripeTransactionRepository _stripeTransactionRepository;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly IBoothRepository _boothRepository;
        private readonly IPromotionRepository _promotionRepository;
        private readonly ICartRepository _cartRepository;
        private readonly PromotionManager _promotionManager;
        private readonly ILogger<StripeWebhookHandler> _logger;

        public StripeWebhookHandler(
            ISettingProvider settingProvider,
            IStripeTransactionRepository stripeTransactionRepository,
            IRepository<Rental, Guid> rentalRepository,
            IBoothRepository boothRepository,
            IPromotionRepository promotionRepository,
            ICartRepository cartRepository,
            PromotionManager promotionManager,
            ILogger<StripeWebhookHandler> logger)
        {
            _settingProvider = settingProvider;
            _stripeTransactionRepository = stripeTransactionRepository;
            _rentalRepository = rentalRepository;
            _boothRepository = boothRepository;
            _promotionRepository = promotionRepository;
            _cartRepository = cartRepository;
            _promotionManager = promotionManager;
            _logger = logger;
        }

        public async Task<bool> HandleWebhookAsync(string json, string stripeSignature)
        {
            try
            {
                var webhookSecret = await _settingProvider.GetOrNullAsync(MPSettings.PaymentProviders.StripeWebhookSecret);
                if (string.IsNullOrEmpty(webhookSecret))
                {
                    _logger.LogWarning("StripeWebhookHandler: Webhook secret not configured");
                    return false;
                }

                Event stripeEvent;

                try
                {
                    // Verify webhook signature
                    stripeEvent = EventUtility.ConstructEvent(
                        json,
                        stripeSignature,
                        webhookSecret
                    );

                    _logger.LogInformation("StripeWebhookHandler: Verified webhook event {EventId} of type {EventType}",
                        stripeEvent.Id, stripeEvent.Type);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "StripeWebhookHandler: Invalid webhook signature");
                    return false;
                }

                // Handle the event
                switch (stripeEvent.Type)
                {
                    case "checkout.session.completed":
                        await HandleCheckoutSessionCompletedAsync(stripeEvent);
                        break;

                    case "payment_intent.succeeded":
                        await HandlePaymentIntentSucceededAsync(stripeEvent);
                        break;

                    case "payment_intent.payment_failed":
                        await HandlePaymentIntentFailedAsync(stripeEvent);
                        break;

                    case "payment_intent.canceled":
                        await HandlePaymentIntentCanceledAsync(stripeEvent);
                        break;

                    default:
                        _logger.LogInformation("StripeWebhookHandler: Unhandled event type: {EventType}", stripeEvent.Type);
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeWebhookHandler: Error handling webhook");
                return false;
            }
        }

        private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session == null)
            {
                _logger.LogWarning("StripeWebhookHandler: Invalid session object in checkout.session.completed event");
                return;
            }

            _logger.LogInformation("StripeWebhookHandler: Checkout session {SessionId} completed. Payment status: {PaymentStatus}, PaymentIntentId: {PaymentIntentId}",
                session.Id, session.PaymentStatus, session.PaymentIntentId);

            // Log session metadata for debugging
            if (session.Metadata != null && session.Metadata.Count > 0)
            {
                session.Metadata.TryGetValue("session_id", out var sessionId);
                session.Metadata.TryGetValue("merchant_id", out var merchantId);
                _logger.LogInformation("StripeWebhookHandler: Session metadata - SessionId: {SessionId}, MerchantId: {MerchantId}",
                    sessionId, merchantId);
            }

            // If payment is completed, process the payment
            if (session.PaymentStatus == "paid")
            {
                await ProcessSuccessfulPaymentAsync(session.Id, session.AmountTotal ?? 0);
            }
        }

        private async Task HandlePaymentIntentSucceededAsync(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null)
            {
                _logger.LogWarning("StripeWebhookHandler: Invalid payment intent object in payment_intent.succeeded event");
                return;
            }

            _logger.LogInformation("StripeWebhookHandler: Payment intent {PaymentIntentId} succeeded. Amount: {Amount} {Currency}",
                paymentIntent.Id, paymentIntent.Amount / 100m, paymentIntent.Currency);

            // Log payment intent metadata for debugging
            if (paymentIntent.Metadata != null && paymentIntent.Metadata.Count > 0)
            {
                paymentIntent.Metadata.TryGetValue("session_id", out var sessionId);
                paymentIntent.Metadata.TryGetValue("merchant_id", out var merchantId);
                _logger.LogInformation("StripeWebhookHandler: Payment intent metadata - SessionId: {SessionId}, MerchantId: {MerchantId}",
                    sessionId, merchantId);
            }

            // Update transaction in database if it exists
            try
            {
                var transaction = await _stripeTransactionRepository.GetByPaymentIntentIdAsync(paymentIntent.Id);
                if (transaction != null)
                {
                    transaction.Status = "succeeded";
                    transaction.CompletedAt = DateTime.UtcNow;

                    await _stripeTransactionRepository.UpdateAsync(transaction);
                    _logger.LogInformation("StripeWebhookHandler: Updated transaction for payment intent {PaymentIntentId}",
                        paymentIntent.Id);
                }
                else
                {
                    _logger.LogInformation("StripeWebhookHandler: No existing transaction found for payment intent {PaymentIntentId} - this is normal for Checkout Session flow",
                        paymentIntent.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeWebhookHandler: Error updating transaction for payment intent {PaymentIntentId}",
                    paymentIntent.Id);
            }
        }

        private async Task HandlePaymentIntentFailedAsync(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null)
            {
                _logger.LogWarning("StripeWebhookHandler: Invalid payment intent object in payment_intent.payment_failed event");
                return;
            }

            _logger.LogWarning("StripeWebhookHandler: Payment intent {PaymentIntentId} failed. Error: {ErrorMessage}",
                paymentIntent.Id, paymentIntent.LastPaymentError?.Message);

            // Update transaction in database if it exists
            try
            {
                var transaction = await _stripeTransactionRepository.GetByPaymentIntentIdAsync(paymentIntent.Id);
                if (transaction != null)
                {
                    transaction.Status = "failed";
                    // Error details are logged above but not stored in transaction entity

                    await _stripeTransactionRepository.UpdateAsync(transaction);
                    _logger.LogInformation("StripeWebhookHandler: Updated transaction for failed payment intent {PaymentIntentId}",
                        paymentIntent.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeWebhookHandler: Error updating transaction for payment intent {PaymentIntentId}",
                    paymentIntent.Id);
            }
        }

        private async Task HandlePaymentIntentCanceledAsync(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null)
            {
                _logger.LogWarning("StripeWebhookHandler: Invalid payment intent object in payment_intent.canceled event");
                return;
            }

            _logger.LogInformation("StripeWebhookHandler: Payment intent {PaymentIntentId} canceled", paymentIntent.Id);

            // Update transaction in database if it exists
            try
            {
                var transaction = await _stripeTransactionRepository.GetByPaymentIntentIdAsync(paymentIntent.Id);
                if (transaction != null)
                {
                    transaction.Status = "cancelled";

                    await _stripeTransactionRepository.UpdateAsync(transaction);
                    _logger.LogInformation("StripeWebhookHandler: Updated transaction for cancelled payment intent {PaymentIntentId}",
                        paymentIntent.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeWebhookHandler: Error updating transaction for payment intent {PaymentIntentId}",
                    paymentIntent.Id);
            }
        }

        /// <summary>
        /// Process successful payment by updating rental and booth status
        /// </summary>
        private async Task ProcessSuccessfulPaymentAsync(string stripeSessionId, long amountTotal)
        {
            try
            {
                _logger.LogInformation("StripeWebhookHandler: Processing successful payment for session {SessionId}, amount {Amount}",
                    stripeSessionId, amountTotal / 100m);

                // Find ALL rentals associated with this Stripe Session ID
                // Rentals are linked via Payment.Przelewy24TransactionId (reused for all providers)
                var rentals = await _rentalRepository.GetListAsync(r =>
                    r.Payment.Przelewy24TransactionId == stripeSessionId);

                if (rentals.Count == 0)
                {
                    _logger.LogWarning("StripeWebhookHandler: No rentals found for Stripe session {SessionId}", stripeSessionId);
                    return;
                }

                _logger.LogInformation("StripeWebhookHandler: Found {Count} rental(s) for session {SessionId}",
                    rentals.Count, stripeSessionId);

                // Verify total amount matches
                var totalExpectedAmount = rentals.Sum(r => r.Payment.TotalAmount);
                var actualAmount = amountTotal / 100m; // Convert from cents

                if (Math.Abs(totalExpectedAmount - actualAmount) > 0.01m)
                {
                    _logger.LogError("StripeWebhookHandler: Amount mismatch for session {SessionId}. Expected: {Expected}, Actual: {Actual}",
                        stripeSessionId, totalExpectedAmount, actualAmount);
                    return;
                }

                var paidDate = DateTime.UtcNow;

                // Mark all rentals as paid and their booths as rented
                foreach (var rental in rentals)
                {
                    // Mark rental as paid
                    rental.MarkAsPaid(rental.Payment.TotalAmount, paidDate, stripeSessionId);

                    // Change booth status from Reserved to Rented
                    var booth = await _boothRepository.GetAsync(rental.BoothId);
                    booth.MarkAsRented();
                    await _boothRepository.UpdateAsync(booth);

                    await _rentalRepository.UpdateAsync(rental);

                    _logger.LogInformation("StripeWebhookHandler: Payment confirmed for rental {RentalId}. Booth {BoothId} marked as rented.",
                        rental.Id, rental.BoothId);
                }

                // Update Stripe transaction status
                var transaction = await _stripeTransactionRepository.GetByPaymentIntentIdAsync(stripeSessionId);
                if (transaction != null)
                {
                    transaction.SetStatus("succeeded");
                    transaction.CompletedAt = paidDate;
                    await _stripeTransactionRepository.UpdateAsync(transaction);
                }

                // Register promotion usage if promotion was applied to the cart
                await RegisterPromotionUsageAsync(stripeSessionId, rentals[0].Payment);

                _logger.LogInformation("StripeWebhookHandler: Payment confirmed for {Count} rental(s), session {SessionId}",
                    rentals.Count, stripeSessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeWebhookHandler: Error processing successful payment for session {SessionId}", stripeSessionId);
            }
        }

        /// <summary>
        /// Register promotion usage after successful payment
        /// </summary>
        private async Task RegisterPromotionUsageAsync(string stripeSessionId, MP.Domain.Rentals.Payment payment)
        {
            try
            {
                // Extract promotion data from payment metadata
                // The metadata was set when creating the payment in CartAppService
                if (payment == null)
                {
                    _logger.LogWarning("StripeWebhookHandler: Payment is null for session {SessionId}", stripeSessionId);
                    return;
                }

                // Check if payment has promotion metadata
                // Note: We need to query Stripe session to get metadata since it's not stored in our Payment entity
                // As a workaround, we'll get the cart from the rentalIds associated with this payment

                // Try to reconstruct from payment and rental relationship
                var rentalIds = await _rentalRepository
                    .GetListAsync(r => r.Payment.Przelewy24TransactionId == stripeSessionId);

                if (!rentalIds.Any())
                {
                    _logger.LogInformation("StripeWebhookHandler: No rentals found for promotion registration, session {SessionId}", stripeSessionId);
                    return;
                }

                // Get first rental to find associated cart
                var firstRental = rentalIds.First();

                // Find cart items that reference these rentals
                var allCarts = await _cartRepository.GetListAsync();

                var relevantCart = allCarts.FirstOrDefault(c =>
                    c.Items.Any(item => rentalIds.Select(r => r.Id).Contains(item.RentalId ?? Guid.Empty)));

                if (relevantCart == null)
                {
                    _logger.LogInformation("StripeWebhookHandler: No cart found for promotion registration, session {SessionId}", stripeSessionId);
                    return;
                }

                // Check if cart had promotion applied
                if (!relevantCart.HasPromotionApplied() || !relevantCart.AppliedPromotionId.HasValue)
                {
                    _logger.LogInformation("StripeWebhookHandler: Cart {CartId} has no promotion applied, session {SessionId}",
                        relevantCart.Id, stripeSessionId);
                    return;
                }

                // Register promotion usage
                await _promotionManager.RecordUsageAsync(
                    relevantCart.AppliedPromotionId.Value,
                    firstRental.UserId,
                    relevantCart.Id,
                    relevantCart.DiscountAmount,
                    relevantCart.GetTotalAmount(),
                    relevantCart.GetFinalAmount(),
                    relevantCart.PromoCodeUsed,
                    firstRental.Id
                );

                _logger.LogInformation("StripeWebhookHandler: Registered promotion usage for cart {CartId}, promotion {PromotionId}, session {SessionId}",
                    relevantCart.Id, relevantCart.AppliedPromotionId, stripeSessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeWebhookHandler: Error registering promotion usage for session {SessionId}", stripeSessionId);
                // Don't throw - payment already succeeded, we just couldn't register promo usage
                // This will be caught and logged but won't fail the webhook
            }
        }
    }
}
