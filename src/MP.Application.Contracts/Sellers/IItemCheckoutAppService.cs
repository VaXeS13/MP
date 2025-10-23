using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MP.Application.Contracts.Sellers
{
    /// <summary>
    /// Application service for seller checkout operations
    /// </summary>
    public interface IItemCheckoutAppService : IApplicationService
    {
        /// <summary>
        /// Find an item by barcode
        /// </summary>
        Task<ItemForCheckoutDto?> FindItemByBarcodeAsync(FindItemByBarcodeDto input);

        /// <summary>
        /// Get available payment methods for current tenant
        /// </summary>
        Task<AvailablePaymentMethodsDto> GetAvailablePaymentMethodsAsync();

        /// <summary>
        /// Checkout an item with specified payment method
        /// </summary>
        Task<CheckoutResultDto> CheckoutItemAsync(CheckoutItemDto input);

        /// <summary>
        /// Checkout multiple items with specified payment method
        /// </summary>
        Task<CheckoutResultDto> CheckoutItemsAsync(CheckoutItemsDto input);

        /// <summary>
        /// Calculate checkout summary for multiple items
        /// </summary>
        Task<CheckoutSummaryDto> CalculateCheckoutSummaryAsync(List<Guid> itemIds);

        /// <summary>
        /// Check terminal status
        /// </summary>
        Task<bool> CheckTerminalStatusAsync();
    }
}