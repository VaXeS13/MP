using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.Payments
{
    public interface IPayPalTransactionRepository : ITransactionRepository<PayPalTransaction>
    {
        /// <summary>
        /// Get transaction by PayPal Order ID
        /// </summary>
        Task<PayPalTransaction?> GetByOrderIdAsync(string orderId);

        /// <summary>
        /// Get transaction by session ID (Order ID)
        /// This is an alias for GetByOrderIdAsync since Order ID is used as session ID
        /// </summary>
        Task<PayPalTransaction?> FindBySessionIdAsync(string sessionId);

        /// <summary>
        /// Get transaction by PayPal Payment ID (legacy API)
        /// </summary>
        Task<PayPalTransaction?> GetByPaymentIdAsync(string paymentId);

        /// <summary>
        /// Get transaction by Capture ID
        /// </summary>
        Task<PayPalTransaction?> GetByCaptureIdAsync(string captureId);

        /// <summary>
        /// Get transactions by Payer ID
        /// </summary>
        Task<List<PayPalTransaction>> GetByPayerIdAsync(string payerId);

        /// <summary>
        /// Get transactions by environment (sandbox/live)
        /// </summary>
        Task<List<PayPalTransaction>> GetByEnvironmentAsync(string environment);

        /// <summary>
        /// Get transactions with disputes
        /// </summary>
        Task<List<PayPalTransaction>> GetDisputedTransactionsAsync();

        /// <summary>
        /// Get refunded transactions
        /// </summary>
        Task<List<PayPalTransaction>> GetRefundedTransactionsAsync();

        /// <summary>
        /// Get cancelled transactions count for monitoring
        /// </summary>
        Task<int> GetCancelledTransactionsCountAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Get transactions by funding source (paypal, card, credit, etc.)
        /// </summary>
        Task<List<PayPalTransaction>> GetByFundingSourceAsync(string fundingSource);

        /// <summary>
        /// Get average transaction amount for analytics
        /// </summary>
        Task<decimal> GetAverageTransactionAmountAsync(DateTime fromDate, DateTime toDate);
    }
}