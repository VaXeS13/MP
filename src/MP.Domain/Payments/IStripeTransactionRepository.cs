using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.Payments
{
    public interface IStripeTransactionRepository : ITransactionRepository<StripeTransaction>
    {
        /// <summary>
        /// Get transaction by Stripe PaymentIntent ID
        /// </summary>
        Task<StripeTransaction?> GetByPaymentIntentIdAsync(string paymentIntentId);

        /// <summary>
        /// Get transaction by session ID (Checkout Session ID or PaymentIntent ID)
        /// This is an alias for GetByPaymentIntentIdAsync since Checkout Session ID
        /// is stored in PaymentIntentId field
        /// </summary>
        Task<StripeTransaction?> FindBySessionIdAsync(string sessionId);

        /// <summary>
        /// Get successful transactions within date range
        /// </summary>
        Task<List<StripeTransaction>> GetSuccessfulTransactionsAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Get transactions by charge ID
        /// </summary>
        Task<StripeTransaction?> GetByChargeIdAsync(string chargeId);

        /// <summary>
        /// Get transactions by customer ID
        /// </summary>
        Task<List<StripeTransaction>> GetByCustomerIdAsync(string customerId);
    }
}