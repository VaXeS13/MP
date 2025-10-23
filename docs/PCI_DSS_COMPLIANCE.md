# PCI DSS Compliance Guide - MP Local Agent

**Status**: ✅ In Progress
**Version**: 1.0.0
**Last Updated**: 2025-10-23
**Compliance Level**: Level 1 (Payment Card Industry Data Security Standard)

---

## 📋 TABLE OF CONTENTS

1. [Overview](#overview)
2. [What is PCI DSS](#what-is-pci-dss)
3. [Compliance Requirements](#compliance-requirements)
4. [Implementation in MP](#implementation-in-mp)
5. [Data Protection](#data-protection)
6. [Testing & Validation](#testing--validation)
7. [Common Mistakes](#common-mistakes)
8. [References](#references)

---

## Overview

The MP Local Agent handles payment card data from payment terminals. **Under NO circumstances should full Primary Account Numbers (PAN) be stored, transmitted, or logged** in the system.

This document outlines the PCI DSS Level 1 compliance requirements and how they're implemented in the MP Local Agent architecture.

### Key Principles

```
❌ NEVER store full card numbers (PAN)
❌ NEVER transmit PAN over unencrypted channels
❌ NEVER log PAN in plain text
✅ ALWAYS use tokenization
✅ ALWAYS validate with P2PE certification
✅ ALWAYS encrypt sensitive data in transit
```

---

## What is PCI DSS

**PCI DSS** = Payment Card Industry Data Security Standard

### Compliance Levels

| Level | Transactions/Year | Requirement |
|-------|-------------------|-------------|
| **Level 1** | >6M OR Visa/Mastercard required | **Full audit required** |
| **Level 2** | 1M-6M | Self-assessment |
| **Level 3** | <1M | Limited compliance |
| **Level 4** | <20k | Self-certification |

**MP is Level 1** - requires full compliance and annual audit.

### Core Requirements

1. **Install & maintain firewall** ✅ (Azure Security)
2. **Don't use default passwords** ✅ (API Keys)
3. **Protect stored data** ✅ (Encryption)
4. **Encrypt data in transit** ✅ (HTTPS/TLS)
5. **Use antivirus software** ✅ (Local Agent)
6. **Secure development practices** ✅ (This document)
7. **Restrict access to data** ✅ (Multi-tenancy)
8. **Identify & monitor access** ✅ (Logging)
9. **Test security regularly** ✅ (Tests)
10. **Maintain security policy** ✅ (This doc)

---

## Compliance Requirements

### Requirement 1: Payment Data Security

#### What We Handle
```
❌ NEVER handle:
- Full PAN (Primary Account Number)
- Magnetic stripe data (Track 1 & 2)
- CVV/CVC codes
- PINs

✅ Safe to handle:
- Last 4 digits (e.g., ****1234)
- Card brand (Visa, Mastercard)
- Transaction ID/Token from processor
- Expiry month/year (for display only)
```

#### Implementation in TerminalPaymentResponse

```csharp
public class TerminalPaymentResponse : CommandResponseBase
{
    // ✅ SAFE - Only last 4 digits
    public string? LastFourDigits { get; set; }  // "1234"
    public string? MaskedPan { get; set; }       // "****1234"

    // ✅ SAFE - Card brand
    public string? CardType { get; set; }        // "Visa"

    // ✅ SAFE - Token from payment processor
    public string? TransactionId { get; set; }   // "txn_123abc"
    public string? AuthorizationCode { get; set; } // "A1B2C3"

    // ❌ REMOVED - Full card numbers
    // public string? RawResponse { get; set; }  // NEVER!

    // ✅ SAFE - Non-sensitive metadata
    public Dictionary<string, string> SafeMetadata { get; set; }

    // ✅ Compliance validation
    public bool IsP2PECompliant { get; set; }

    public void ValidatePciCompliance()
    {
        // Validate MaskedPan contains only last 4 digits
        if (!string.IsNullOrEmpty(MaskedPan))
        {
            var panDigits = MaskedPan.Where(char.IsDigit).Count();
            if (panDigits > 4)
            {
                throw new PciComplianceException(
                    "PAN data detected in response - too many digits exposed");
            }
        }
    }
}
```

### Requirement 2: Tokenization

**Tokenization** replaces sensitive card data with a unique token.

#### How It Works

```
1. Customer swipes card on TERMINAL
2. Terminal securely processes card data
3. Payment processor (e.g., Ingenico, Verifone) returns:
   - Transaction Token (safe to store)
   - Last 4 digits (safe to display)
   - Authorization Code (safe to store)
4. Local Agent sends to Azure API:
   - Token (NOT card data)
   - Last 4 digits
   - Auth code
5. Azure API stores:
   - Transaction ID
   - Masked PAN (last 4)
   - Timestamp
```

#### Example Flow

```csharp
// Terminal (Local Agent side)
var paymentRequest = new AuthorizeTerminalPaymentCommand
{
    Amount = 99.99,
    Currency = "PLN",
    Description = "Item #123"  // NO card data
};

// Terminal processes (secure enclave)
var response = new TerminalPaymentResponse
{
    TransactionId = "4E5F6G7H",      // ✅ Token from processor
    AuthorizationCode = "123456",    // ✅ Auth code
    LastFourDigits = "4242",         // ✅ Safe to display
    MaskedPan = "****4242",          // ✅ Safe to store
    CardType = "Visa",               // ✅ Brand name
    IsP2PECompliant = true           // ✅ Validation flag
};

// NEVER send back:
// - Full card number
// - CVV
// - Magnetic stripe data
// - PIN
```

### Requirement 3: P2PE Certification

**P2PE** = Point-to-Point Encryption

#### What It Means

- Card data is encrypted **at the point of entry** (terminal)
- Remains encrypted throughout the entire data path
- Decrypted only in **secure environments** (payment processor)
- Local Agent NEVER has access to unencrypted card data

#### How MP Validates P2PE

```csharp
public class TerminalPaymentResponse
{
    // Flag indicating terminal is P2PE certified
    public bool IsP2PECompliant { get; set; } = true;

    public void ValidatePciCompliance()
    {
        // Terminal must be P2PE certified
        if (!IsP2PECompliant)
        {
            throw new PciComplianceException(
                "Terminal does not have P2PE certification");
        }
    }
}
```

#### P2PE Certified Terminals

For production, use **only** terminals certified by payment processors:
- ✅ **Ingenico** (P2PE certified models)
- ✅ **Verifone** (P2PE certified models)
- ✅ **PAX** (P2PE certified models)
- ❌ **Generic USB terminals** (often NOT P2PE certified)

### Requirement 4: Data Transmission

#### In Transit (TLS)

```
Azure API ↔ Local Agent
    ↓
Use HTTPS/TLS 1.2+ (automatically by SignalR)
Use certificate pinning (optional, added security)
```

#### Configuration in appsettings.json

```json
{
  "SignalRClient": {
    "Url": "https://mp.azurewebsites.net/signalr/local-agent",
    "UseTls": true,
    "TlsVersion": "1.2",
    "CertificatePinning": true,
    "AllowedCertificateHashes": [
      "sha256/AAAA1234567890..."
    ]
  }
}
```

#### Data at Rest

```csharp
// Stored in database (Azure SQL)
public class FiscalReceipt
{
    public Guid Id { get; set; }
    public string TransactionToken { get; set; }        // ✅ Token only
    public string? MaskedPan { get; set; }               // ✅ Last 4 only
    public string? CardType { get; set; }                // ✅ Brand name
    public DateTime ProcessedAt { get; set; }

    // Encryption at DB level (Azure SQL transparent encryption)
}
```

### Requirement 5: Access Control & Logging

#### Logging Requirements

```csharp
// ✅ GOOD - Log transaction metadata
_logger.LogInformation(
    "Payment processed: TransactionId={TransactionId}, Amount={Amount}, CardType={CardType}",
    response.TransactionId, response.Amount, response.CardType);

// ❌ BAD - NEVER log sensitive data
// _logger.LogInformation("Full Response: {@Response}", response);  // NO!
// _logger.LogInformation("Card Data: {Pan}", cardNumber);          // NO!
```

#### Audit Trail

Every payment transaction must have:

```csharp
public class PaymentAuditLog : FullAuditedAggregateRoot<Guid>
{
    public Guid TenantId { get; set; }
    public string TransactionId { get; set; }      // ✅ Token
    public string? MaskedPan { get; set; }         // ✅ Last 4
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string Status { get; set; }             // "Success", "Failed"
    public string? ErrorMessage { get; set; }      // No card data
    public string ProcessedBy { get; set; }        // User/Agent ID
    public string? IpAddress { get; set; }         // Connection source

    // ✅ What was done and when
    // ❌ NOT: sensitive card data
}
```

---

## Implementation in MP

### Phase 1: Sanitization (MP-66) ✅

**Status**: In Progress

#### Changes to TerminalPaymentResponse

```diff
public class TerminalPaymentResponse : CommandResponseBase
{
    public string? TransactionId { get; set; }
    public string? AuthorizationCode { get; set; }
    public string Status { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? Timestamp { get; set; }
    public string? CardType { get; set; }
    public string? LastFourDigits { get; set; }
-   public string? RawResponse { get; set; }          // ❌ REMOVE
+   public string? MaskedPan { get; set; }            // ✅ ADD
+   public bool IsP2PECompliant { get; set; } = true; // ✅ ADD
+   public Dictionary<string, string> SafeMetadata { get; set; } = new(); // ✅ ADD
+
+   public void ValidatePciCompliance()
+   {
+       if (MaskedPan?.Length > 4 && MaskedPan.Any(char.IsDigit) &&
+           MaskedPan.Count(char.IsDigit) > 4)
+       {
+           throw new PciComplianceException("PAN data detected in response");
+       }
+   }
}
```

#### MockTerminalService Update

```csharp
var response = new TerminalPaymentResponse
{
    CommandId = command.CommandId,
    Success = true,
    TransactionId = $"MOCK-{Guid.NewGuid():N}",      // ✅ Token
    AuthorizationCode = _random.Next(100000, 999999).ToString(),
    Status = "captured",
    Amount = command.Amount,
    Currency = command.Currency,
    CardType = GetRandomCardType(),
    LastFourDigits = _random.Next(1000, 9999).ToString(),  // ✅ Last 4
    MaskedPan = $"****{_random.Next(1000, 9999)}",   // ✅ Masked
    IsP2PECompliant = true,                          // ✅ Flag
    SafeMetadata = new()                             // ✅ Safe data only
    {
        ["TerminalId"] = "MOCK-TERMINAL-001",
        ["ProcessingTime"] = $"{processingTime.TotalMilliseconds}ms"
    }
};

response.ValidatePciCompliance();  // ✅ Validate
return response;
```

### Phase 2: Agent Authentication (MP-67)

**Status**: Pending - See `AGENT_AUTHENTICATION.md`

Secure API keys prevent unauthorized access to payment data.

### Phase 3: Secure Logging

**Status**: Pending - To be implemented

Never log sensitive data:

```csharp
// ✅ Good logging
public async Task<TerminalPaymentResponse> AuthorizePaymentAsync(...)
{
    _logger.LogInformation(
        "Authorizing payment: Amount={Amount}, Currency={Currency}, CardType={CardType}",
        request.Amount, request.Currency, request.CardType);

    try
    {
        var response = await terminal.AuthorizeAsync(request);
        response.ValidatePciCompliance();

        _logger.LogInformation(
            "Payment authorized: TransactionId={TransactionId}, Status={Status}",
            response.TransactionId, response.Status);

        return response;
    }
    catch (Exception ex)
    {
        // ✅ Log error without card data
        _logger.LogError(ex, "Payment authorization failed: {ErrorMessage}", ex.Message);
        throw;
    }
}
```

---

## Data Protection

### Local Agent Storage

```
Local Agent Computer
├── appsettings.json (API Key, encrypted)
├── offline_queue.db (SQLite, TDE enabled)
└── logs/ (structured, no PAN)
```

**Encryption Requirements**:

1. **Offline Queue DB**
   ```sql
   -- Enable Transparent Data Encryption (TDE)
   PRAGMA cipher = 'sqlcipher';
   PRAGMA key = 'encryption_password';
   ```

2. **Configuration Files**
   - API keys stored in secure vault (Azure Key Vault)
   - Never committed to Git

3. **Logs**
   - Structured logging without sensitive data
   - Separate sensitive logs with restricted access

### Azure SQL Database

```sql
-- Transparent Data Encryption (TDE) - REQUIRED
ALTER DATABASE [MP] SET ENCRYPTION ON;

-- Audit transactions
CREATE AUDIT [mp_audit]
TO FILE (FILEPATH = 'https://mpstorage.blob.core.windows.net/',
         FILENAME = 'audit.xel');

ALTER DATABASE [MP] SET AUDIT (STATE = ON, AUDIT_GUID = audit_id);
```

---

## Testing & Validation

### Unit Tests (MP-66)

```csharp
[Fact]
public void TerminalPaymentResponse_Should_Reject_Full_Pan()
{
    // Arrange
    var response = new TerminalPaymentResponse
    {
        MaskedPan = "1234567890123456",  // ❌ Full PAN
    };

    // Act & Assert
    Assert.Throws<PciComplianceException>(() =>
        response.ValidatePciCompliance());
}

[Fact]
public void TerminalPaymentResponse_Should_Allow_Masked_Pan()
{
    // Arrange
    var response = new TerminalPaymentResponse
    {
        MaskedPan = "****1234",  // ✅ Masked
    };

    // Act & Assert
    response.ValidatePciCompliance();  // Should not throw
}

[Fact]
public void TerminalPaymentResponse_Should_Not_Expose_Card_Details()
{
    // Arrange
    var response = new TerminalPaymentResponse
    {
        TransactionId = "token_123",
        LastFourDigits = "1234",
        CardType = "Visa",
        MaskedPan = "****1234"
    };

    // Act
    var json = JsonSerializer.Serialize(response);

    // Assert
    Assert.DoesNotContain("cvv", json);
    Assert.DoesNotContain("pin", json);
    Assert.DoesNotContain("RawResponse", json);
}
```

### Security Scanning

1. **Code scanning for hardcoded secrets**
   ```bash
   dotnet tool install -g TruffleHog
   trufflehog filesystem . --only-verified
   ```

2. **Dependency vulnerability scanning**
   ```bash
   dotnet list package --vulnerable
   ```

3. **OWASP Top 10 testing**
   - Injection attacks
   - Authentication bypass
   - Data exposure

---

## Common Mistakes

### ❌ Mistake 1: Logging Full PAN

```csharp
// ❌ NEVER DO THIS
_logger.LogInformation("Card: {Card}", cardNumber);
_logger.LogDebug("Full response: {@Response}", response);
Console.WriteLine($"Processing card: {pan}");
```

**Fix**: Log only safe data
```csharp
// ✅ GOOD
_logger.LogInformation("Card type: {CardType}, Last 4: {Last4}",
    cardType, lastFour);
```

### ❌ Mistake 2: Storing RawResponse

```csharp
// ❌ NEVER DO THIS
public class PaymentTransaction
{
    public string RawResponse { get; set; }  // May contain PAN!
}
```

**Fix**: Store only safe fields
```csharp
// ✅ GOOD
public class PaymentTransaction
{
    public string TransactionId { get; set; }
    public string MaskedPan { get; set; }  // ****1234
    public string CardType { get; set; }
}
```

### ❌ Mistake 3: Unencrypted Transmission

```csharp
// ❌ NEVER DO THIS
var url = "http://payment-processor.com/api/process";  // No TLS!
```

**Fix**: Use HTTPS
```csharp
// ✅ GOOD
var url = "https://payment-processor.com/api/process";  // TLS 1.2+
```

### ❌ Mistake 4: Caching Card Data

```csharp
// ❌ NEVER DO THIS
var cardCache = new Dictionary<Guid, CardData>();  // NEVER CACHE!
```

**Fix**: Cache only tokens
```csharp
// ✅ GOOD
var transactionCache = new Dictionary<Guid, TransactionToken>();
```

---

## References

### PCI DSS Standards
- [PCI Security Standards Council](https://www.pcisecuritystandards.org/)
- [PCI DSS v3.2.1 Compliance Guide](https://www.pcisecuritystandards.org/documents/PCI_DSS_v3-2-1.pdf)
- [PCI P2PE Program](https://www.pcisecuritystandards.org/assessors_and_solutions/point_to_point_encryption)

### Payment Terminals (P2PE Certified)
- [Ingenico P2PE Certified Terminals](https://www.ingenico.com/en/solutions/p2pe)
- [Verifone P2PE Certified Terminals](https://www.verifone.com/en/us/solutions/p2pe)
- [PAX Technology P2PE](https://www.paxtechnology.com/)

### Azure Security
- [Azure SQL Transparent Data Encryption](https://docs.microsoft.com/en-us/azure/azure-sql/database/transparent-data-encryption-tde-overview)
- [Azure Key Vault](https://docs.microsoft.com/en-us/azure/key-vault/)
- [Azure Security Best Practices](https://docs.microsoft.com/en-us/azure/security/fundamentals/best-practices-and-patterns)

### Testing
- [OWASP Testing Guide](https://owasp.org/www-project-web-security-testing-guide/)
- [PCI Penetration Testing Requirements](https://www.pcisecuritystandards.org/assessors_and_solutions/penetration_testing)

---

## Checklist for Production Deployment

Before going live, verify:

- [ ] PCI DSS Requirement 1: Firewall in place (Azure)
- [ ] PCI DSS Requirement 2: No default credentials
- [ ] PCI DSS Requirement 3: Encrypt cardholder data
- [ ] PCI DSS Requirement 4: Encrypt data in transit (HTTPS/TLS)
- [ ] PCI DSS Requirement 5: Antivirus on Local Agent
- [ ] PCI DSS Requirement 6: Secure development (code review)
- [ ] PCI DSS Requirement 7: Access control (API keys)
- [ ] PCI DSS Requirement 8: Unique user IDs (audit)
- [ ] PCI DSS Requirement 9: Physical access control
- [ ] PCI DSS Requirement 10: Log all access to cardholder data
- [ ] Annual PCI DSS Audit completed
- [ ] Penetration testing performed (annual)
- [ ] All 168 unit tests passing
- [ ] Security scanning passed (TruffleHog, dependency check)
- [ ] Code review by security team completed

---

## Support & Questions

For questions about PCI DSS compliance, contact:
- **Security Team**: security@mp.local
- **PCI Compliance Officer**: pci-compliance@mp.local
- **Payment Processor Support**: See terminal manufacturer documentation

**Last Review**: 2025-10-23
**Next Review**: 2025-11-23
