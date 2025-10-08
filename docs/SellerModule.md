# Seller Module Documentation

## Overview

The Seller Module provides functionality for in-store sales using barcode scanning and payment terminal integration. This module supports:

- **Barcode Scanning**: Find items by scanning barcodes using external USB scanners
- **Cash Payments**: Manual cash payment processing
- **Card Payments**: Integration with payment terminals (Ingenico, Verifone, Stripe Terminal, SumUp, Adyen, etc.)
- **Multi-tenancy**: Per-tenant terminal configuration

## Architecture

### Backend Components

#### Domain Layer

**Entities:**
- `TenantTerminalSettings` (`MP.Domain.Terminals`): Per-tenant payment terminal configuration
  - `ProviderId`: Terminal provider identifier (e.g., "mock", "ingenico", "stripe_terminal")
  - `ConfigurationJson`: Provider-specific settings (API keys, merchant IDs, etc.)
  - `Currency`: Transaction currency
  - `IsEnabled`: Enable/disable terminal
  - `IsSandbox`: Sandbox/production mode

**Updated Entities:**
- `RentalItem` (`MP.Domain.Rentals`): Added `Barcode` property for barcode scanning
  - Added navigation property to `Rental` for customer info lookup

**Interfaces:**
- `ITerminalPaymentProvider` (`MP.Domain.Terminals`): Generic interface for payment terminal providers
  - `AuthorizePaymentAsync`: Authorize card payment
  - `CapturePaymentAsync`: Capture authorized payment
  - `RefundPaymentAsync`: Refund payment
  - `CancelPaymentAsync`: Cancel/void payment
  - `GetPaymentStatusAsync`: Check payment status
  - `CheckTerminalStatusAsync`: Verify terminal connectivity

#### Application Layer

**Services:**
- `ItemCheckoutAppService` (`MP.Application.Sellers`): Main checkout logic
  - `FindItemByBarcodeAsync`: Search items by barcode
  - `GetAvailablePaymentMethodsAsync`: Get configured payment methods for tenant
  - `CheckoutItemAsync`: Process cash or card payment
  - `CheckTerminalStatusAsync`: Check terminal availability

**Terminal Providers:**
- `MockTerminalProvider` (`MP.Application.Terminals`): Development/testing mock provider
  - Simulates terminal behavior with 95% success rate
  - 1.5s processing delay to simulate real terminal
  - No actual hardware required

**Factory:**
- `TerminalPaymentProviderFactory` (`MP.Application.Terminals`): Provider resolution
  - Manages all registered terminal providers
  - Resolves provider by tenant configuration
  - Initializes providers with tenant-specific settings

**DTOs:**
- `FindItemByBarcodeDto`: Barcode search request
- `ItemForCheckoutDto`: Item details for checkout
- `CheckoutItemDto`: Checkout request with payment method
- `CheckoutResultDto`: Checkout result
- `AvailablePaymentMethodsDto`: Available payment methods
- `PaymentMethodType` enum: Cash (0), Card (1)

#### API Layer

**Controllers:**
- `SellerController` (`MP.HttpApi.Controllers`): REST API endpoints
  - `POST /api/app/seller/find-by-barcode`: Find item by barcode
  - `GET /api/app/seller/payment-methods`: Get available payment methods
  - `POST /api/app/seller/checkout`: Process checkout
  - `GET /api/app/seller/terminal-status`: Check terminal status

#### Database

**Migration:**
- `Add_SellerModule_BarcodesAndTerminals`: Adds:
  - `Barcode` column to `RentalItems` table
  - `TenantTerminalSettings` table with indexes

### Frontend Components

#### Angular Components

**SellerCheckoutComponent** (`angular/src/app/seller-checkout`):
- Standalone component with PrimeNG UI
- Keyboard-friendly barcode scanning
- Real-time item lookup
- Payment method selection
- Transaction confirmation

**Features:**
- Autofocus on barcode input for seamless scanning
- Enter key triggers search
- Visual feedback for item status
- Disabled state handling during processing
- Toast notifications for user feedback
- Confirmation dialog before checkout

**Services:**
- `SellerService` (`angular/src/app/proxy/sellers`): API communication
  - Generated Angular proxy for type-safe API calls
  - Observable-based async operations

**Models:**
- TypeScript interfaces matching backend DTOs
- Enum for PaymentMethodType

**Routing:**
- `/seller-checkout`: Seller checkout page (requires authentication)

## Configuration

### Backend Setup

1. **Run Database Migration:**
   ```bash
   dotnet run --project src/MP.DbMigrator/MP.DbMigrator.csproj
   ```

2. **Configure Terminal Provider (Per Tenant):**

   Add a record to `TenantTerminalSettings` table:
   ```sql
   INSERT INTO AppTenantTerminalSettings
   (Id, TenantId, ProviderId, ConfigurationJson, Currency, IsEnabled, IsSandbox, CreationTime)
   VALUES
   (NEWID(), 'your-tenant-id', 'mock', '{}', 'PLN', 1, 1, GETUTCDATE());
   ```

3. **Add Terminal Provider:**

   To add a new provider (e.g., Stripe Terminal):

   ```csharp
   // 1. Implement ITerminalPaymentProvider
   public class StripeTerminalProvider : ITerminalPaymentProvider
   {
       public string ProviderId => "stripe_terminal";
       public string DisplayName => "Stripe Terminal";
       public string Description => "Stripe payment terminal integration";

       // Implement all interface methods...
   }

   // 2. Register in TerminalPaymentProviderFactory
   _allProviders = new List<ITerminalPaymentProvider>
   {
       _serviceProvider.GetRequiredService<MockTerminalProvider>(),
       _serviceProvider.GetRequiredService<StripeTerminalProvider>() // Add here
   };
   ```

### Frontend Setup

The Angular component is already configured. Access at:
```
http://localhost:4200/seller-checkout
```

## Usage

### Barcode Scanning

1. **Connect USB Barcode Scanner:**
   - Any USB barcode scanner that acts as a keyboard (HID) will work
   - No driver installation needed

2. **Assign Barcodes to Items:**
   ```csharp
   var item = await _rentalItemRepository.GetAsync(itemId);
   item.SetBarcode("1234567890123");
   await _rentalItemRepository.UpdateAsync(item);
   ```

3. **Scan in Seller Checkout:**
   - Open `/seller-checkout` page
   - Focus is automatically on barcode input
   - Scan barcode or type manually
   - Press Enter or click "Find"

### Cash Checkout Flow

1. Scan item barcode
2. Verify item details and price
3. Click "Cash" button
4. Confirm in dialog
5. Transaction recorded with `CASH-{guid}` transaction ID
6. Item marked as sold

### Card Checkout Flow

1. Scan item barcode
2. Verify item details and price
3. Click "Card" button
4. Confirm in dialog
5. Terminal processes payment (customer inserts/taps card)
6. System receives authorization
7. Payment auto-captured
8. Item marked as sold
9. Transaction ID from terminal stored

## Testing

### Mock Terminal Testing

The mock terminal provider is enabled by default in development:

**Success Scenario (95% probability):**
- Simulates 1.5s processing delay
- Returns authorization code
- Auto-captures payment
- Generates mock card details (VISA, last 4 digits)

**Failure Scenario (5% probability):**
- Simulates declined card
- Returns error: "INSUFFICIENT_FUNDS"
- Payment not captured
- Item remains available

### Manual Testing Steps

1. **Setup Test Data:**
   ```sql
   -- Add barcode to a test item
   UPDATE AppRentalItems
   SET Barcode = '1234567890'
   WHERE Id = 'your-test-item-id';

   -- Ensure item has price set
   UPDATE AppRentalItems
   SET ActualPrice = 100.00, CommissionPercentage = 10.0
   WHERE Id = 'your-test-item-id';

   -- Configure mock terminal
   INSERT INTO AppTenantTerminalSettings
   (Id, TenantId, ProviderId, ConfigurationJson, Currency, IsEnabled, IsSandbox)
   VALUES
   (NEWID(), NULL, 'mock', '{}', 'PLN', 1, 1);
   ```

2. **Test Barcode Scanning:**
   - Navigate to `/seller-checkout`
   - Type barcode: `1234567890`
   - Press Enter
   - Verify item details appear

3. **Test Cash Payment:**
   - Follow barcode scanning
   - Click "Cash" button
   - Confirm dialog
   - Verify success message
   - Check database: `Status = 'Sold'`, `SoldAt` timestamp

4. **Test Card Payment:**
   - Follow barcode scanning
   - Click "Card" button
   - Confirm dialog
   - Wait for mock terminal (1.5s)
   - Verify success message
   - Check transaction ID in success message

## Integration with Existing System

### Payment Recording

The seller module does NOT update the `Payment` value object on `Rental` entity. It only:
- Marks `RentalItem` as sold
- Sets `SoldAt` timestamp
- Records transaction ID (for card payments)

If you need to track seller payments separately, extend the implementation:

```csharp
// Create a new entity for seller transactions
public class SellerTransaction : Entity<Guid>
{
    public Guid RentalItemId { get; set; }
    public PaymentMethodType PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public string? TransactionId { get; set; }
    public DateTime ProcessedAt { get; set; }
}
```

### Commission Calculation

Use existing methods on `RentalItem`:
- `GetCommissionAmount()`: Calculate commission from sold item
- `GetCustomerAmount()`: Calculate amount owed to customer

### Reporting

Query sold items:
```csharp
var soldItems = await _rentalItemRepository
    .GetQueryableAsync()
    .Where(x => x.Status == RentalItemStatus.Sold)
    .Where(x => x.SoldAt >= startDate && x.SoldAt <= endDate)
    .ToListAsync();

var totalSales = soldItems.Sum(x => x.ActualPrice ?? 0);
var totalCommission = soldItems.Sum(x => x.GetCommissionAmount());
```

## Security Considerations

1. **Terminal Configuration:**
   - Store sensitive API keys in `ConfigurationJson` encrypted
   - Use ABP Setting Management for production
   - Never commit real API keys to source control

2. **Access Control:**
   - Add permissions for seller operations
   - Restrict `/seller-checkout` route to authorized users
   - Add audit logging for all transactions

3. **Transaction Integrity:**
   - All checkouts are transactional (database + terminal)
   - Failures roll back both operations
   - Log all payment attempts (success + failure)

## Future Enhancements

### Phase 2 - Real Terminal Providers

Implement providers for:
- **Ingenico**: Popular in Europe
- **Verifone**: Global provider
- **Stripe Terminal**: Modern, developer-friendly
- **SumUp**: Mobile terminals
- **Adyen**: Enterprise solution

### Phase 3 - Advanced Features

- **Refund Support**: Process refunds through terminal
- **Receipt Printing**: Generate and print receipts
- **Offline Mode**: Queue transactions when terminal offline
- **Multi-item Checkout**: Scan multiple items in one transaction
- **Discount Support**: Apply discounts at checkout
- **Batch Closing**: End-of-day terminal reconciliation

### Phase 4 - Admin UI

Create management interface for:
- Terminal configuration per tenant
- Transaction history and reporting
- Failed payment retry
- Terminal status monitoring
- Barcode generation and printing

## Troubleshooting

### Barcode Scanner Not Working

**Issue:** Scanner not sending input

**Solution:**
- Verify scanner is configured as HID keyboard
- Check scanner is set to correct barcode format (EAN-13, Code 128, etc.)
- Ensure focus is on barcode input field
- Test scanner in a text editor first

### Terminal Not Available

**Issue:** "Card payments are not configured"

**Solution:**
```sql
-- Check terminal configuration
SELECT * FROM AppTenantTerminalSettings
WHERE TenantId = 'your-tenant-id' OR TenantId IS NULL;

-- Verify IsEnabled = 1
-- Verify ProviderId matches registered provider
```

### Payment Declined

**Issue:** Mock terminal randomly declines

**Solution:**
- This is expected (5% failure rate simulates real-world)
- For guaranteed success, modify `MockTerminalProvider` line 59:
  ```csharp
  var isSuccess = random.Next(100) < 100; // Changed from 95 to 100
  ```

### Item Not Found

**Issue:** Barcode scan returns "Not Found"

**Solution:**
```sql
-- Verify barcode exists
SELECT * FROM AppRentalItems WHERE Barcode = 'scanned-barcode';

-- Ensure item status is ForSale
-- Check item belongs to current tenant (if multi-tenant)
```

## API Reference

### Find Item by Barcode

```http
POST /api/app/seller/find-by-barcode
Content-Type: application/json

{
  "barcode": "1234567890123"
}
```

**Response:**
```json
{
  "id": "guid",
  "rentalId": "guid",
  "name": "Item Name",
  "description": "Description",
  "category": "Category",
  "photoUrl": "https://...",
  "barcode": "1234567890123",
  "actualPrice": 100.00,
  "commissionPercentage": 10.0,
  "status": "ForSale",
  "customerName": "John Doe",
  "customerEmail": "john@example.com",
  "customerPhone": "+48123456789"
}
```

### Get Available Payment Methods

```http
GET /api/app/seller/payment-methods
```

**Response:**
```json
{
  "cashEnabled": true,
  "cardEnabled": true,
  "terminalProviderId": "mock",
  "terminalProviderName": "Mock Terminal (Development)"
}
```

### Checkout Item

```http
POST /api/app/seller/checkout
Content-Type: application/json

{
  "rentalItemId": "guid",
  "paymentMethod": 0,  // 0 = Cash, 1 = Card
  "amount": 100.00
}
```

**Response:**
```json
{
  "success": true,
  "transactionId": "CASH-abc123...",
  "paymentMethod": 0,
  "amount": 100.00,
  "processedAt": "2025-09-30T12:34:56Z"
}
```

**Error Response:**
```json
{
  "success": false,
  "errorMessage": "Card declined - insufficient funds",
  "paymentMethod": 1,
  "amount": 100.00,
  "processedAt": "2025-09-30T12:34:56Z"
}
```

## License

This module is part of the MP project. All rights reserved.