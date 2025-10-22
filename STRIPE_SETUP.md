# Stripe Integration - Setup and Configuration Guide

## Overview

This application uses Stripe Checkout Sessions for payment processing. This guide covers complete setup, configuration, and testing procedures.

## Current Status ✅

### What's Already Implemented:
- ✅ Stripe Provider implementation (`StripeProvider.cs`)
- ✅ Webhook handler (`StripeWebhookHandler.cs`)
- ✅ Payment controller with webhook endpoint
- ✅ Frontend payment selection and checkout flow
- ✅ Test API keys configured in `appsettings.json`
- ✅ Sample products created in Stripe account

### What Needs Configuration:
- ⚠️ **Webhook Secret** (CRITICAL for production)
- ⚠️ Local webhook testing setup (for development)
- ℹ️ Production API keys (when ready to go live)

## Stripe Account Information

**Test Account Details:**
- Account ID: `acct_1SEbgBQihiXumQfX`
- Display Name: "New business sandbox"
- Mode: Test Mode
- Dashboard: https://dashboard.stripe.com/test

**Test API Keys (already configured):**
- Publishable Key: `pk_test_51SEbgBQihiXumQfX...`
- Secret Key: `sk_test_51SEbgBQihiXumQfX...`

**Products Created:**
- Product ID: `prod_TH7YyaTAAWdDl2`
- Name: "Booth Rental"
- Description: "Rental of a booth space for flea market vendors"
- Prices:
  - 100 PLN: `price_1SKZJnQihiXumQfXnpKhUmXJ`
  - 200 PLN: `price_1SKZJoQihiXumQfXmhKfwwht`
  - 300 PLN: `price_1SKZJpQihiXumQfX9D07XlQR`

**Test Payment Intent Found:**
- ID: `pi_3SEc2XQihiXumQfX0uRJteyF`
- Amount: 16.00 PLN
- Status: succeeded ✅

## Architecture

### Payment Flow:

```
User                Frontend           Backend API          Stripe
  |                    |                    |                  |
  |-- Select Booth --->|                    |                  |
  |-- Add to Cart ---->|                    |                  |
  |-- Checkout ------->|                    |                  |
  |                    |-- POST /checkout ->|                  |
  |                    |                    |-- Create Session->|
  |                    |<-- Payment URL ----|<-- Session URL --|
  |<-- Redirect -------|                    |                  |
  |                                         |                  |
  |========== Stripe Checkout Page ========|==================|
  |                                         |                  |
  |-- Pay with Card ------------------------|----------------->|
  |<-- Success/Cancel ----------------------|------------------|
  |                                         |                  |
  |                    |<-- Redirect -------|<-- Webhook ------|
  |<-- Success Page ---|                    |                  |
  |                    |                    |-- Update Rental->|
  |                    |                    |-- Update Booth ->|
```

### Key Components:

**Backend (C#):**
- `StripeProvider.cs` - Creates Checkout Sessions
- `StripeWebhookHandler.cs` - Handles webhook events
- `PaymentController.cs` - API endpoints
- `StripeTransaction.cs` - Entity for tracking payments

**Frontend (Angular):**
- `CheckoutComponent` - Payment method selection
- `PaymentService` - API communication
- `CartService` - Cart and checkout operations

### Webhook Events Handled:
- `checkout.session.completed` - Payment successful
- `payment_intent.succeeded` - Payment confirmed
- `payment_intent.payment_failed` - Payment failed
- `payment_intent.canceled` - Payment canceled

## Setup Instructions

### Step 1: Configure Webhook Secret (CRITICAL)

**Option A: Using Stripe CLI (Recommended for Development)**

1. Install Stripe CLI:
   ```bash
   # Windows (using Scoop)
   scoop bucket add stripe https://github.com/stripe/scoop-stripe-cli.git
   scoop install stripe

   # macOS (using Homebrew)
   brew install stripe/stripe-cli/stripe

   # Linux
   # Download from: https://github.com/stripe/stripe-cli/releases/latest
   ```

2. Login to Stripe:
   ```bash
   stripe login
   ```

3. Forward webhooks to local API:
   ```bash
   stripe listen --forward-to http://localhost:44377/api/app/payments/stripe/webhook
   ```

4. Copy the webhook signing secret (starts with `whsec_`):
   ```
   > Ready! Your webhook signing secret is whsec_xxxxxxxxxxxxx
   ```

5. Update `appsettings.json`:
   ```json
   "Stripe": {
     "WebhookSecret": "whsec_xxxxxxxxxxxxx"
   }
   ```

6. **OR** update via ABP Settings Management UI:
   - Navigate to: `/SettingManagement`
   - Find: `MP.PaymentProviders.Stripe.WebhookSecret`
   - Set value: `whsec_xxxxxxxxxxxxx`

**Option B: Using ngrok/Cloudflare Tunnel (For Remote Testing)**

1. Start your API server:
   ```bash
   dotnet run --project src/MP.HttpApi.Host/MP.HttpApi.Host.csproj
   ```

2. Create tunnel:
   ```bash
   # Using ngrok
   ngrok http 44377

   # Or using Cloudflare Tunnel
   cloudflared tunnel --url http://localhost:44377
   ```

3. Copy the public URL (e.g., `https://abc123.ngrok.io`)

4. Configure webhook in Stripe Dashboard:
   - Go to: https://dashboard.stripe.com/test/webhooks
   - Click "Add endpoint"
   - Endpoint URL: `https://abc123.ngrok.io/api/app/payments/stripe/webhook`
   - Select events:
     - `checkout.session.completed`
     - `payment_intent.succeeded`
     - `payment_intent.payment_failed`
     - `payment_intent.canceled`
   - Click "Add endpoint"

5. Copy the webhook signing secret and update configuration (same as Option A, step 5-6)

**Option C: Production Setup**

1. Deploy your API to production server with public URL

2. Configure webhook in Stripe Dashboard:
   - Go to: https://dashboard.stripe.com/webhooks (or /test/webhooks for test mode)
   - Endpoint URL: `https://your-domain.com/api/app/payments/stripe/webhook`
   - Select same events as Option B

3. Update production `appsettings.json` or Settings with webhook secret

### Step 2: Enable Stripe Provider

1. **Via Settings Management UI** (Recommended):
   - Navigate to: `/SettingManagement`
   - Enable: `MP.PaymentProviders.Stripe.Enabled` = `true`
   - Verify keys are set:
     - `MP.PaymentProviders.Stripe.PublishableKey`
     - `MP.PaymentProviders.Stripe.SecretKey`
     - `MP.PaymentProviders.Stripe.WebhookSecret`

2. **Via Database** (Alternative):
   ```sql
   UPDATE AbpSettings
   SET Value = 'True'
   WHERE Name = 'MP.PaymentProviders.Stripe.Enabled';
   ```

### Step 3: Test Payment Flow

1. Start backend API:
   ```bash
   dotnet run --project src/MP.HttpApi.Host/MP.HttpApi.Host.csproj
   ```

2. Start webhook forwarding (if using Stripe CLI):
   ```bash
   stripe listen --forward-to http://localhost:44377/api/app/payments/stripe/webhook
   ```

3. Start Angular frontend:
   ```bash
   cd angular
   ng serve
   ```

4. Test the flow:
   - Navigate to: http://localhost:4200
   - Login with test account
   - Browse booths and add one to cart
   - Go to cart and click "Checkout"
   - Select "Stripe" as payment provider
   - Select "Credit/Debit Card" as payment method
   - Click "Complete Checkout"
   - You'll be redirected to Stripe Checkout page

5. Use test cards:
   - **Success**: `4242 4242 4242 4242`
   - **3D Secure**: `4000 0025 0000 3155`
   - **Declined**: `4000 0000 0000 9995`
   - **CVV**: Any 3 digits
   - **Expiry**: Any future date
   - **ZIP**: Any 5 digits

6. After payment:
   - Check Stripe CLI output for webhook events
   - Check API logs for webhook processing
   - Verify rental status changed to "Paid"
   - Verify booth status changed to "Rented"

## Testing Checklist

- [ ] Webhook secret configured
- [ ] Stripe provider enabled in settings
- [ ] Backend API running
- [ ] Webhook forwarding active (Stripe CLI or ngrok)
- [ ] Frontend running
- [ ] Successfully add booth to cart
- [ ] Successfully select Stripe payment
- [ ] Successfully redirect to Stripe Checkout
- [ ] Successfully complete payment with test card
- [ ] Webhook received and processed
- [ ] Rental marked as paid
- [ ] Booth status changed to rented
- [ ] Cart cleared after successful payment

## Implementation Details

### How It Works:

1. **Cart Checkout**:
   - User clicks "Complete Checkout" in cart
   - Frontend calls: `POST /api/carts/checkout`
   - Backend creates Draft rentals for each cart item
   - Backend calls `StripeProvider.CreatePaymentAsync()`

2. **Stripe Checkout Session**:
   - Creates session with line items (dynamic pricing)
   - Sets metadata: `merchant_id`, `session_id`, `tenant_id`
   - Returns checkout URL
   - Frontend redirects user to Stripe

3. **User Pays**:
   - User enters card details on Stripe Checkout page
   - Stripe processes payment
   - User redirected back to success/cancel URL

4. **Webhook Processing**:
   - Stripe sends `checkout.session.completed` webhook
   - `StripeWebhookHandler` verifies signature
   - Finds rentals by session ID (stored in `Payment.Przelewy24TransactionId`)
   - Verifies payment amount matches
   - Marks rentals as paid
   - Changes booth status to Rented
   - Registers promotion usage (if applicable)

### Database Schema:

**Rental.Payment (Value Object):**
```csharp
- TotalAmount: decimal
- Currency: string
- PaidDate: DateTime?
- Przelewy24TransactionId: string  // Reused for all providers (Stripe Session ID)
```

**StripeTransaction (Entity):**
```csharp
- PaymentIntentId: string
- AmountCents: long
- Amount: decimal
- Currency: string
- Status: string
- RentalId: Guid?
- CompletedAt: DateTime?
```

### Settings Configuration:

All settings are managed via ABP Settings system with per-tenant support:

```csharp
MP.PaymentProviders.Stripe.Enabled           // bool
MP.PaymentProviders.Stripe.PublishableKey    // string
MP.PaymentProviders.Stripe.SecretKey         // string
MP.PaymentProviders.Stripe.WebhookSecret     // string
```

## Known Issues and Limitations

### Current Limitations:

1. **Field Naming**: `Przelewy24TransactionId` used for all providers
   - This field stores Stripe Session ID
   - **Impact**: Confusing naming but works correctly
   - **Fix**: Consider renaming to `ExternalTransactionId` in future migration

2. **No Products in Stripe**: Application creates dynamic prices
   - **Impact**: Less organized in Stripe Dashboard
   - **Status**: Sample products now created (see above)
   - **Note**: Application still uses dynamic pricing, products are for reference

3. **Webhook Secret Placeholder**: Default config has placeholder value
   - **Impact**: CRITICAL - webhooks won't work without configuration
   - **Fix**: Follow Step 1 above to configure

4. **No Idempotency Keys**: Checkout session creation lacks idempotency
   - **Impact**: Duplicate sessions possible on retry
   - **Recommendation**: Add idempotency key (e.g., `cart_id + timestamp`)

### Webhook Considerations:

- **Localhost Development**: Requires Stripe CLI or tunnel
- **Signature Verification**: Always enabled - don't bypass in production
- **Retry Logic**: Stripe retries failed webhooks automatically
- **Event Order**: Not guaranteed - use idempotency
- **Timeout**: Webhook handler should respond within 5 seconds

## Troubleshooting

### Problem: Webhooks not received

**Symptoms:**
- Payment succeeds but rental not marked as paid
- Booth status doesn't change to Rented

**Solutions:**
1. Check webhook secret is configured correctly
2. Verify webhook forwarding is active (Stripe CLI)
3. Check API logs for webhook processing errors
4. Verify endpoint is accessible: `POST /api/app/payments/stripe/webhook`
5. Check Stripe Dashboard > Webhooks for delivery status

### Problem: Payment fails with "Stripe is not configured"

**Symptoms:**
- Error when selecting Stripe provider
- "Stripe configuration is incomplete" message

**Solutions:**
1. Verify Stripe is enabled in settings
2. Check API keys are set (PublishableKey and SecretKey)
3. Restart API server after changing settings
4. Check tenant-specific settings if using multi-tenancy

### Problem: Amount mismatch error in webhook

**Symptoms:**
- Webhook receives event but doesn't process
- "Amount mismatch" error in logs

**Solutions:**
1. Check cart items haven't changed between checkout and payment
2. Verify promotions haven't expired during checkout
3. Check currency conversion if using different currencies
4. Clear cart and try again

### Problem: 3D Secure cards not working

**Symptoms:**
- Payment requires authentication but fails
- User stuck on authentication page

**Solutions:**
1. Use test card that requires authentication: `4000 0025 0000 3155`
2. Complete authentication on Stripe's test page
3. Check return URLs are configured correctly
4. Verify browser allows popups from Stripe

## Monitoring and Analytics

### Stripe Dashboard:
- Payments: https://dashboard.stripe.com/test/payments
- Webhooks: https://dashboard.stripe.com/test/webhooks
- Logs: https://dashboard.stripe.com/test/logs
- Events: https://dashboard.stripe.com/test/events

### Application Logs:
Check these log messages in `Logs/logs.txt`:
```
StripeProvider: Creating payment for amount {Amount} {Currency}
StripeProvider: Created Stripe Checkout Session {SessionId}
StripeWebhookHandler: Verified webhook event {EventId} of type {EventType}
StripeWebhookHandler: Payment confirmed for rental {RentalId}
```

### Database Queries:

**Check recent payments:**
```sql
SELECT TOP 10
    r.Id as RentalId,
    r.BoothId,
    r.Status,
    r.Payment_TotalAmount,
    r.Payment_Currency,
    r.Payment_PaidDate,
    r.Payment_Przelewy24TransactionId as StripeSessionId
FROM Rentals r
WHERE r.Payment_Przelewy24TransactionId LIKE 'cs_%'  -- Stripe session IDs
ORDER BY r.CreationTime DESC;
```

**Check Stripe transactions:**
```sql
SELECT TOP 10
    PaymentIntentId,
    Amount,
    Currency,
    Status,
    RentalId,
    CompletedAt,
    CreationTime
FROM StripeTransactions
ORDER BY CreationTime DESC;
```

## Production Checklist

Before going live:

- [ ] Replace test API keys with live keys
- [ ] Configure production webhook endpoint
- [ ] Test with real card (small amount)
- [ ] Set up error monitoring (e.g., Sentry)
- [ ] Configure webhook retry alerts
- [ ] Review Stripe fees and pricing
- [ ] Set up payout schedule in Stripe
- [ ] Configure tax settings if applicable
- [ ] Update terms of service for payment processing
- [ ] Test refund flow
- [ ] Document support procedures for payment issues

## Additional Resources

- [Stripe Checkout Documentation](https://stripe.com/docs/payments/checkout)
- [Stripe Webhooks Guide](https://stripe.com/docs/webhooks)
- [Stripe CLI](https://stripe.com/docs/stripe-cli)
- [Stripe Test Cards](https://stripe.com/docs/testing)
- [ABP Settings System](https://docs.abp.io/en/abp/latest/Settings)

## Support

For issues with:
- **Stripe Integration**: Contact development team
- **Stripe Account**: https://support.stripe.com
- **ABP Framework**: https://docs.abp.io/en/abp/latest

---

**Last Updated**: 2025-01-20
**Version**: 1.0
**Status**: ✅ Ready for Testing (Webhook configuration required)
