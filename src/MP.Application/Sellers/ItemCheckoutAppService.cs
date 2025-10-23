using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using MP.Application.Contracts.Sellers;
using MP.Application.Contracts.Services;
using MP.Application.Terminals;
using MP.Application.FiscalPrinters;
using MP.Domain.Rentals;
using MP.Domain.Terminals;
using MP.Domain.FiscalPrinters;
using MP.Domain.Items;
using MP.Domain.Items.Events;
using MP.Rentals;

namespace MP.Application.Sellers
{
    public class ItemCheckoutAppService : ApplicationService, IItemCheckoutAppService
    {
        private readonly IRepository<ItemSheetItem, Guid> _itemSheetItemRepository;
        private readonly IRepository<Item, Guid> _itemRepository;
        private readonly IRepository<ItemSheet, Guid> _itemSheetRepository;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly ITerminalPaymentProviderFactory _terminalFactory;
        private readonly IFiscalPrinterProviderFactory _fiscalPrinterFactory;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ILogger<ItemCheckoutAppService> _logger;
        private readonly ILocalEventBus _localEventBus;

        public ItemCheckoutAppService(
            IRepository<ItemSheetItem, Guid> itemSheetItemRepository,
            IRepository<Item, Guid> itemRepository,
            IRepository<ItemSheet, Guid> itemSheetRepository,
            IRepository<Rental, Guid> rentalRepository,
            ITerminalPaymentProviderFactory terminalFactory,
            IFiscalPrinterProviderFactory fiscalPrinterFactory,
            ISignalRNotificationService signalRNotificationService,
            ILogger<ItemCheckoutAppService> logger,
            ILocalEventBus localEventBus)
        {
            _itemSheetItemRepository = itemSheetItemRepository;
            _itemRepository = itemRepository;
            _itemSheetRepository = itemSheetRepository;
            _rentalRepository = rentalRepository;
            _terminalFactory = terminalFactory;
            _fiscalPrinterFactory = fiscalPrinterFactory;
            _signalRNotificationService = signalRNotificationService;
            _logger = logger;
            _localEventBus = localEventBus;
        }

        public async Task<ItemForCheckoutDto?> FindItemByBarcodeAsync(FindItemByBarcodeDto input)
        {
            if (string.IsNullOrWhiteSpace(input.Barcode))
            {
                throw new UserFriendlyException("Barcode cannot be empty");
            }

            // Use projection to load only required fields instead of full User entity
            var queryable = await _itemSheetItemRepository.GetQueryableAsync();
            var itemDto = await queryable
                .AsNoTracking()
                .Where(x => x.Barcode == input.Barcode.Trim())
                .Select(x => new ItemForCheckoutDto
                {
                    Id = x.Id,
                    RentalId = x.ItemSheet.RentalId ?? Guid.Empty,
                    Name = x.Item.Name,
                    Description = null,
                    Category = x.Item.Category,
                    PhotoUrl = null,
                    Barcode = x.Barcode,
                    ActualPrice = x.Item.Price,
                    CommissionPercentage = x.CommissionPercentage,
                    CommissionAmount = x.Item.Price * (x.CommissionPercentage / 100m),
                    CustomerAmount = x.Item.Price - (x.Item.Price * (x.CommissionPercentage / 100m)),
                    Status = x.Status.ToString(),
                    CustomerName = x.ItemSheet.Rental != null && x.ItemSheet.Rental.User != null
                        ? (x.ItemSheet.Rental.User.Name ?? x.ItemSheet.Rental.User.UserName ?? "Unknown")
                        : "Unknown",
                    CustomerEmail = x.ItemSheet.Rental != null ? x.ItemSheet.Rental.User.Email : null,
                    CustomerPhone = x.ItemSheet.Rental != null ? x.ItemSheet.Rental.User.PhoneNumber : null
                })
                .FirstOrDefaultAsync();

            if (itemDto == null)
            {
                _logger.LogWarning("Item with barcode {Barcode} not found", input.Barcode);
                return null;
            }

            return itemDto;
        }

        public async Task<AvailablePaymentMethodsDto> GetAvailablePaymentMethodsAsync()
        {
            var result = new AvailablePaymentMethodsDto
            {
                CashEnabled = true,
                CardEnabled = false
            };

            try
            {
                // Get active terminal
                var provider = await _terminalFactory.GetActiveProviderAsync(CurrentTenant.Id);

                if (provider != null)
                {
                    result.CardEnabled = true;
                    result.TerminalProviderId = provider.ProviderId;
                    result.TerminalProviderName = provider.DisplayName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available payment methods for tenant {TenantId}", CurrentTenant.Id);
            }

            return result;
        }

        public async Task<CheckoutSummaryDto> CalculateCheckoutSummaryAsync(List<Guid> itemIds)
        {
            if (itemIds == null || !itemIds.Any())
            {
                throw new UserFriendlyException("Item IDs cannot be empty");
            }

            var queryable = await _itemSheetItemRepository.GetQueryableAsync();
            var items = await queryable
                .AsNoTracking()
                .Where(x => itemIds.Contains(x.Id) && x.Status == ItemSheetItemStatus.ForSale)
                .Select(x => new ItemForCheckoutDto
                {
                    Id = x.Id,
                    RentalId = x.ItemSheet.RentalId ?? Guid.Empty,
                    Name = x.Item.Name,
                    Description = null,
                    Category = x.Item.Category,
                    PhotoUrl = null,
                    Barcode = x.Barcode,
                    ActualPrice = x.Item.Price,
                    CommissionPercentage = x.CommissionPercentage,
                    CommissionAmount = x.Item.Price * (x.CommissionPercentage / 100m),
                    CustomerAmount = x.Item.Price - (x.Item.Price * (x.CommissionPercentage / 100m)),
                    Status = x.Status.ToString(),
                    CustomerName = x.ItemSheet.Rental != null && x.ItemSheet.Rental.User != null
                        ? (x.ItemSheet.Rental.User.Name ?? x.ItemSheet.Rental.User.UserName ?? "Unknown")
                        : "Unknown",
                    CustomerEmail = x.ItemSheet.Rental != null ? x.ItemSheet.Rental.User.Email : null,
                    CustomerPhone = x.ItemSheet.Rental != null ? x.ItemSheet.Rental.User.PhoneNumber : null
                })
                .ToListAsync();

            var summary = new CheckoutSummaryDto
            {
                Items = items,
                ItemsCount = items.Count,
                TotalAmount = items.Sum(x => x.ActualPrice ?? 0),
                TotalCommission = items.Sum(x => x.CommissionAmount),
                TotalCustomerAmount = items.Sum(x => x.CustomerAmount)
            };

            return summary;
        }

        public async Task<CheckoutResultDto> CheckoutItemsAsync(CheckoutItemsDto input)
        {
            if (input.ItemSheetItemIds == null || !input.ItemSheetItemIds.Any())
            {
                throw new UserFriendlyException("No items provided for checkout");
            }

            // Get all items
            var queryable = await _itemSheetItemRepository.GetQueryableAsync();
            var items = await queryable
                .Include(x => x.Item)
                .Include(x => x.ItemSheet)
                .Where(x => input.ItemSheetItemIds.Contains(x.Id))
                .ToListAsync();

            if (!items.Any())
            {
                throw new UserFriendlyException("No items found");
            }

            // Validate all items are for sale
            var invalidItems = items.Where(x => x.Status != ItemSheetItemStatus.ForSale).ToList();
            if (invalidItems.Any())
            {
                throw new UserFriendlyException($"Some items are not available for sale. Status: {string.Join(", ", invalidItems.Select(x => x.Status))}");
            }

            // Calculate expected total
            var expectedTotal = items.Sum(x => x.Item.Price);
            if (Math.Abs(input.TotalAmount - expectedTotal) > 0.01m)
            {
                throw new UserFriendlyException($"Total amount mismatch. Expected: {expectedTotal}, Received: {input.TotalAmount}");
            }

            string? transactionId = null;

            try
            {
                // Handle payment based on method
                if (input.PaymentMethod == PaymentMethodType.Cash)
                {
                    transactionId = $"CASH-{Guid.NewGuid():N}";
                    _logger.LogInformation(
                        "Processing cash payment for {ItemCount} items - Total Amount: {Amount}",
                        items.Count, input.TotalAmount);
                }
                else if (input.PaymentMethod == PaymentMethodType.Card)
                {
                    // Get ACTIVE terminal provider
                    var provider = await _terminalFactory.GetActiveProviderAsync(CurrentTenant.Id);
                    if (provider == null)
                    {
                        throw new UserFriendlyException("Card payments are not configured for this location. Please configure an active terminal.");
                    }

                    var settings = await _terminalFactory.GetActiveTerminalSettingsAsync(CurrentTenant.Id);

                    // Process card payment on active terminal
                    var paymentRequest = new TerminalPaymentRequest
                    {
                        Amount = input.TotalAmount,
                        Currency = settings?.Currency ?? "PLN",
                        Description = $"Sale of {items.Count} items",
                        RentalItemId = items.First().Id,
                        RentalItemName = $"Multiple items ({items.Count})",
                        Metadata = new()
                        {
                            ["itemCount"] = items.Count.ToString(),
                            ["itemIds"] = string.Join(",", input.ItemSheetItemIds)
                        }
                    };

                    _logger.LogInformation(
                        "Processing card payment on {Provider} terminal for {ItemCount} items",
                        provider.DisplayName, items.Count);

                    var paymentResult = await provider.AuthorizePaymentAsync(paymentRequest);

                    if (!paymentResult.Success)
                    {
                        _logger.LogWarning(
                            "Card payment declined for {ItemCount} items - Error: {Error}",
                            items.Count, paymentResult.ErrorMessage);

                        return new CheckoutResultDto
                        {
                            Success = false,
                            ErrorMessage = paymentResult.ErrorMessage ?? "Payment declined",
                            PaymentMethod = input.PaymentMethod,
                            Amount = input.TotalAmount,
                            ProcessedAt = DateTime.UtcNow
                        };
                    }

                    transactionId = paymentResult.TransactionId;

                    // Auto-capture the payment
                    if (paymentResult.Status == "authorized" || paymentResult.Status == "Authorized")
                    {
                        var captureResult = await provider.CapturePaymentAsync(transactionId!, input.TotalAmount);
                        if (!captureResult.Success)
                        {
                            _logger.LogError(
                                "Failed to capture payment {TransactionId} for {ItemCount} items",
                                transactionId, items.Count);

                            throw new UserFriendlyException("Payment authorized but capture failed. Please contact support.");
                        }
                    }

                    _logger.LogInformation(
                        "Card payment successful for {ItemCount} items - Transaction: {TransactionId}",
                        items.Count, transactionId);
                }

                // Mark all items as sold
                foreach (var item in items)
                {
                    item.MarkAsSold(DateTime.UtcNow);
                }
                await _itemSheetItemRepository.UpdateManyAsync(items);

                // Send notifications for each unique customer
                var uniqueRentals = items
                    .Where(x => x.ItemSheet?.RentalId.HasValue == true)
                    .Select(x => x.ItemSheet!.RentalId!.Value)
                    .Distinct()
                    .ToList();

                foreach (var rentalId in uniqueRentals)
                {
                    var rental = await _rentalRepository.GetAsync(rentalId);
                    var customerItems = items.Where(x => x.ItemSheet?.RentalId == rentalId).ToList();

                    await _signalRNotificationService.SendItemSoldNotificationAsync(
                        rental.UserId,
                        customerItems.First().Id,
                        $"Multiple items ({customerItems.Count})",
                        input.TotalAmount
                    );

                    // Publish ItemSoldEvent for each item
                    foreach (var item in customerItems)
                    {
                        await _localEventBus.PublishAsync(new ItemSoldEvent
                        {
                            UserId = rental.UserId,
                            ItemId = item.Id,
                            ItemName = item.Item.Name ?? "Item",
                            Price = item.Item.Price,
                            Currency = "PLN",
                            SoldAt = DateTime.UtcNow,
                            RentalId = rental.Id
                        });
                    }
                }

                // Refresh dashboard for admins
                await _signalRNotificationService.SendDashboardRefreshAsync(CurrentTenant.Id);

                _logger.LogInformation(
                    "Successfully checked out {ItemCount} items with {PaymentMethod} - Transaction: {TransactionId}",
                    items.Count, input.PaymentMethod, transactionId);

                // Print fiscal receipt if configured
                await PrintFiscalReceiptAsync(items, input.TotalAmount, input.PaymentMethod.ToString(), transactionId);

                return new CheckoutResultDto
                {
                    Success = true,
                    TransactionId = transactionId,
                    PaymentMethod = input.PaymentMethod,
                    Amount = input.TotalAmount,
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch checkout of {ItemCount} items", items.Count);
                throw new UserFriendlyException("An error occurred during checkout. Please try again.");
            }
        }

        public async Task<CheckoutResultDto> CheckoutItemAsync(CheckoutItemDto input)
        {
            var queryable = await _itemSheetItemRepository.GetQueryableAsync();
            var itemSheetItem = await queryable
                .Include(x => x.Item)
                .Include(x => x.ItemSheet)
                .Where(x => x.Id == input.ItemSheetItemId)
                .FirstOrDefaultAsync();

            if (itemSheetItem == null)
            {
                throw new UserFriendlyException("Item not found");
            }

            if (itemSheetItem.Status != ItemSheetItemStatus.ForSale)
            {
                throw new UserFriendlyException($"Item is not available for sale. Current status: {itemSheetItem.Status}");
            }

            var itemPrice = itemSheetItem.Item.Price;

            if (input.Amount != itemPrice)
            {
                throw new UserFriendlyException($"Amount mismatch. Expected: {itemPrice}, Received: {input.Amount}");
            }

            string? transactionId = null;

            try
            {
                // Handle payment based on method
                if (input.PaymentMethod == PaymentMethodType.Cash)
                {
                    transactionId = $"CASH-{Guid.NewGuid():N}";
                    _logger.LogInformation(
                        "Processing cash payment for item {ItemId} - Amount: {Amount}",
                        itemSheetItem.Id, input.Amount);
                }
                else if (input.PaymentMethod == PaymentMethodType.Card)
                {
                    // Get ACTIVE terminal provider
                    var provider = await _terminalFactory.GetActiveProviderAsync(CurrentTenant.Id);
                    if (provider == null)
                    {
                        throw new UserFriendlyException("Card payments are not configured for this location. Please configure an active terminal.");
                    }

                    var settings = await _terminalFactory.GetActiveTerminalSettingsAsync(CurrentTenant.Id);

                    // Process card payment on active terminal
                    var paymentRequest = new TerminalPaymentRequest
                    {
                        Amount = input.Amount,
                        Currency = settings?.Currency ?? "PLN",
                        Description = $"Sale of {itemSheetItem.Item.Name}",
                        RentalItemId = itemSheetItem.Id,
                        RentalItemName = itemSheetItem.Item.Name,
                        Metadata = new()
                        {
                            ["itemSheetItemId"] = itemSheetItem.Id.ToString(),
                            ["rentalId"] = itemSheetItem.ItemSheet.RentalId?.ToString() ?? ""
                        }
                    };

                    _logger.LogInformation(
                        "Processing card payment on {Provider} terminal for item {ItemId}",
                        provider.DisplayName, itemSheetItem.Id);

                    var paymentResult = await provider.AuthorizePaymentAsync(paymentRequest);

                    if (!paymentResult.Success)
                    {
                        _logger.LogWarning(
                            "Card payment declined for item {ItemId} - Error: {Error}",
                            itemSheetItem.Id, paymentResult.ErrorMessage);

                        return new CheckoutResultDto
                        {
                            Success = false,
                            ErrorMessage = paymentResult.ErrorMessage ?? "Payment declined",
                            PaymentMethod = input.PaymentMethod,
                            Amount = input.Amount,
                            ProcessedAt = DateTime.UtcNow
                        };
                    }

                    transactionId = paymentResult.TransactionId;

                    // Auto-capture the payment
                    if (paymentResult.Status == "authorized" || paymentResult.Status == "Authorized")
                    {
                        var captureResult = await provider.CapturePaymentAsync(transactionId!, input.Amount);
                        if (!captureResult.Success)
                        {
                            _logger.LogError(
                                "Failed to capture payment {TransactionId} for item {ItemId}",
                                transactionId, itemSheetItem.Id);

                            throw new UserFriendlyException("Payment authorized but capture failed. Please contact support.");
                        }
                    }

                    _logger.LogInformation(
                        "Card payment successful for item {ItemId} - Transaction: {TransactionId}",
                        itemSheetItem.Id, transactionId);
                }

                // Mark item as sold
                itemSheetItem.MarkAsSold(DateTime.UtcNow);
                await _itemSheetItemRepository.UpdateAsync(itemSheetItem);

                // Send real-time notification to customer via SignalR (legacy)
                Guid? rentalId = null;
                Guid userId = Guid.Empty;

                if (itemSheetItem.ItemSheet.RentalId.HasValue)
                {
                    var rental = await _rentalRepository.GetAsync(itemSheetItem.ItemSheet.RentalId.Value);
                    userId = rental.UserId;
                    rentalId = rental.Id;

                    await _signalRNotificationService.SendItemSoldNotificationAsync(
                        rental.UserId,
                        itemSheetItem.Id,
                        itemSheetItem.Item.Name ?? "Item",
                        input.Amount
                    );

                    // Publish ItemSoldEvent for persistent notification
                    await _localEventBus.PublishAsync(new ItemSoldEvent
                    {
                        UserId = rental.UserId,
                        ItemId = itemSheetItem.Id,
                        ItemName = itemSheetItem.Item.Name ?? "Item",
                        Price = input.Amount,
                        Currency = "PLN",
                        SoldAt = DateTime.UtcNow,
                        RentalId = rental.Id
                    });

                    _logger.LogInformation("Published ItemSoldEvent for user {UserId}, item {ItemId}",
                        rental.UserId, itemSheetItem.Id);
                }

                // Refresh dashboard for admins
                await _signalRNotificationService.SendDashboardRefreshAsync(CurrentTenant.Id);

                _logger.LogInformation(
                    "Item {ItemId} successfully checked out with {PaymentMethod} - Transaction: {TransactionId}",
                    itemSheetItem.Id, input.PaymentMethod, transactionId);

                // Print fiscal receipt if configured
                await PrintFiscalReceiptAsync(itemSheetItem, input.Amount, input.PaymentMethod.ToString(), transactionId);

                return new CheckoutResultDto
                {
                    Success = true,
                    TransactionId = transactionId,
                    PaymentMethod = input.PaymentMethod,
                    Amount = input.Amount,
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout of item {ItemId}", itemSheetItem.Id);
                throw new UserFriendlyException("An error occurred during checkout. Please try again.");
            }
        }

        public async Task<bool> CheckTerminalStatusAsync()
        {
            try
            {
                // Check ACTIVE terminal status
                var provider = await _terminalFactory.GetActiveProviderAsync(CurrentTenant.Id);
                if (provider == null)
                {
                    return false;
                }

                return await provider.CheckTerminalStatusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking terminal status for tenant {TenantId}", CurrentTenant.Id);
                return false;
            }
        }

        private async Task PrintFiscalReceiptAsync(List<ItemSheetItem> items, decimal amount, string paymentMethod, string? transactionId)
        {
            try
            {
                // Get ACTIVE fiscal printer
                var fiscalPrinter = await _fiscalPrinterFactory.GetActiveProviderAsync(CurrentTenant.Id);

                if (fiscalPrinter == null)
                {
                    _logger.LogInformation("No active fiscal printer configured. Skipping fiscal receipt printing.");
                    return;
                }

                _logger.LogInformation(
                    "Printing fiscal receipt on {Provider} for {ItemCount} items",
                    fiscalPrinter.DisplayName, items.Count);

                // Build fiscal receipt request for multiple items
                var receiptItems = new List<FiscalReceiptItem>();
                foreach (var item in items)
                {
                    receiptItems.Add(new FiscalReceiptItem
                    {
                        Name = item.Item.Name ?? "Item",
                        Quantity = 1,
                        UnitPrice = item.Item.Price,
                        TaxRate = "A", // A = 23% VAT in Poland
                        TotalPrice = item.Item.Price
                    });
                }

                var fiscalRequest = new FiscalReceiptRequest
                {
                    Items = receiptItems,
                    TotalAmount = amount,
                    PaymentMethod = paymentMethod,
                    TransactionId = transactionId
                };

                var receiptResult = await fiscalPrinter.PrintReceiptAsync(fiscalRequest);

                if (receiptResult.Success)
                {
                    _logger.LogInformation(
                        "Fiscal receipt printed successfully for {ItemCount} items - Receipt: {FiscalNumber}",
                        items.Count, receiptResult.FiscalNumber);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to print fiscal receipt for {ItemCount} items - Error: {Error}",
                        items.Count, receiptResult.ErrorMessage);
                    // Don't throw exception - sale already completed, fiscal receipt is optional
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing fiscal receipt for {ItemCount} items", items.Count);
                // Don't throw - fiscal receipt printing should not prevent sale completion
            }
        }

        private async Task PrintFiscalReceiptAsync(ItemSheetItem itemSheetItem, decimal amount, string paymentMethod, string? transactionId)
        {
            await PrintFiscalReceiptAsync(new List<ItemSheetItem> { itemSheetItem }, amount, paymentMethod, transactionId);
        }
    }
}