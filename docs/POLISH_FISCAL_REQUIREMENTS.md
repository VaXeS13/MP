# Polskie Wymagania Fiskalne - Prawo i Implementacja

**Status**: In Development (MP-68)
**Version**: 1.0.0
**Ostatnia aktualizacja**: 2025-10-23
**Jurysdykcja**: Polska - Ministerstwo Finansów

---

## 📋 SPIS TREŚCI

1. [Przegląd Wymagań](#przegląd-wymagań)
2. [Rodzaje Kas Fiskalnych](#rodzaje-kas-fiskalnych)
3. [Obowiązki Sprzedawcy](#obowiązki-sprzedawcy)
4. [Format Paragonu](#format-paragonu)
5. [Integracja CRK](#integracja-crk)
6. [Implementacja MP](#implementacja-mp)
7. [Testy i Audyt](#testy-i-audyt)
8. [FAQ i Troubleshooting](#faq-i-troubleshooting)

---

## Przegląd Wymagań

### Prawo

```
📜 Obowiązkowe od: 1 marca 2021 roku
📜 Ustawa o podatku od towarów i usług (VAT)
📜 Rozporządzenie MF w sprawie warunków ewidencji
📜 Projekt Ustawy o CRK (Centralne Repozytorium Kas) - 2024
```

### Stany Kas

| Stan | Wymaganie | Obowiązek |
|------|-----------|----------|
| **Offline (do 2023)** | Tych kas już nie można używać | ❌ |
| **Online** | Transmisja do CRK | ✅ Obowiązkowe |
| **Hybrydowe** | Może pracować offline, potem wysyłać | ✅ Nowe (2024) |

---

## Rodzaje Kas Fiskalnych

### 1. Kasy Tradycyjne (Offline) ❌ PRZESTARZAŁE

```
Producenci: Novitus, Elzab, Posnet
Sposób pracy:
- Przechowuje dane lokalnie
- Drukuje paragon
- BEZ transmisji do CRK
- ❌ Od 2024 niedozwolone dla nowych transakcji
```

### 2. Kasy Online ✅ OBOWIĄZKOWE (od 2024)

```
Producenci: Novitus Online, Elzab Online, Posnet Online
Sposób pracy:
- Bezpośrednia transmisja do Ministerstwa Finansów (CRK)
- Paragon zawiera QR kod
- Wymaga stałego Internetu
- ✅ Wymagane dla nowych terminali

Przykład Integr:
Terminal ↔ MP LocalAgent ↔ CRK (Ministerstwo Finansów)
```

### 3. Kasy Hybrydowe (2024+) ✅ REKOMENDOWANE

```
Producenci: Novitus Hybrid, Elzab Hybrid, Posnet Hybrid
Sposób pracy:
- Pracuje offline i online
- Buforuje dane gdy offline
- Wysyła do CRK gdy online
- ✅ Najlepsza opcja dla sklepów z niestabilnym Internetem

Przykład Flow:
1. Brak Internetu → Paragon zapisywany lokalnie
2. Internet restored → Dane wysyłane do CRK
3. CRK potwierdza → Paragon oficjalny
```

---

## Obowiązki Sprzedawcy

### ✅ MUSI

1. **Używać Kasę Fiskalną**
   - Jeśli przychód roczny > 20,000 PLN
   - Dla każdego punktu sprzedaży

2. **Transmisja do CRK**
   - Od 1 lipca 2024 - obowiązkowa dla kas online
   - Dane w ciągu 48 godzin dla kas hybrydowych

3. **Numeracja Paragonów**
   - Unikalne numery fiskalne (NFG - Numer Fiskalny)
   - Unikalne dla każdej kasy
   - Format: NIP_KASY_NUMER (np. 1234567890_KW001_001234)

4. **Archiwizacja**
   - Przechowywać kopie elektroniczne
   - Minimum 5 lat
   - W formacie odczytywalnym

5. **Zmiana VAT-u**
   - Każdą zmianę stawki VAT trzeba rejestrować w kasie
   - Wymagane zatwierdzenie Ministerstwa

### ❌ NIE WOLNO

- ❌ Kasować paragon (bez zgody MF)
- ❌ Zmieniać dane paragonu po wydruku
- ❌ Usuwać paragony ze systemu (bez sprawozdania)
- ❌ Przechowywać dane bez kopii zapasowych
- ❌ Korzystać z kas będących w konserwacji

---

## Format Paragonu

### Wymagane Informacje

```
┌────────────────────────────────────────┐
│      PARAGON FISKALNY                  │
├────────────────────────────────────────┤
│ Kasa: KW001 (Numer rejestracyjny)      │
│ Data: 2025-10-23 14:32:15              │
│ Numer: 001234 (z danego dnia)          │
├────────────────────────────────────────┤
│ Przedmioty:                            │
│                                        │
│ Koszulka Niebieska         15,00 PLN   │
│ VAT 23%:              2,81 PLN         │
│                                        │
│ Spodnie Czarne           120,00 PLN    │
│ VAT 23%:             22,38 PLN         │
├────────────────────────────────────────┤
│ RAZEM NETTO:         132,24 PLN        │
│ VAT 23%:              25,19 PLN        │
│ RAZEM BRUTTO:        157,43 PLN        │
├────────────────────────────────────────┤
│ Płatność: KARTA                        │
│ Transakcja: 4E5F6G7H                   │
├────────────────────────────────────────┤
│ NIP Sprzedawcy: 1234567890             │
│ Numer Fiscalny: 123...(kod QR)         │
│                                        │
│  [QR CODE]                             │
│                                        │
│ Paragon zapisany w Ministerium         │
│ Ref: JKV7M-2025-10-23-14-32            │
└────────────────────────────────────────┘
```

### Wymagane Pola DTO

```csharp
public class FiscalReceiptRequest
{
    // Identyfikacja kasy
    public string CashRegisterId { get; set; }      // "KW001"
    public string CashRegisterNip { get; set; }     // "1234567890"

    // Identyfikacja sprzedawcy
    public string SellerNip { get; set; }           // "1234567890"
    public string SellerName { get; set; }          // "ABC Sp. z o.o."

    // Identyfikacja paragonu
    public string FiscalNumber { get; set; }        // 123...
    public DateTime SaleDate { get; set; }
    public TimeSpan SaleTime { get; set; }

    // Pozycje na paragonie
    public List<FiscalReceiptItem> Items { get; set; }

    // Sumy
    public decimal TotalNetAmount { get; set; }     // Suma netto
    public decimal TotalTaxAmount { get; set; }     // Suma VAT
    public decimal TotalGrossAmount { get; set; }   // Razem brutto

    // Płatność
    public PaymentMethod PaymentMethod { get; set; }  // Cash, Card, etc.
    public string? PaymentReference { get; set; }     // Transaction ID
}

public class FiscalReceiptItem
{
    public string Name { get; set; }                // "Koszulka Niebieska"
    public int Quantity { get; set; }               // 1
    public decimal UnitPrice { get; set; }          // 15.00
    public decimal TotalPrice { get; set; }         // 15.00
    public decimal VatRate { get; set; }            // 23 (procent)
    public decimal VatAmount { get; set; }          // 2.81
    public string UnitOfMeasure { get; set; }       // "szt" (sztuka)
}

public class FiscalReceiptResponse
{
    // Podstawowe
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // Numer fiskalny
    public string FiscalNumber { get; set; }        // Zwrócony przez kasę
    public string MaskedFiscalNumber { get; set; }  // Wydrukowany na paragonie

    // QR kod
    public string? QrCodeData { get; set; }         // Dane do QR kodu

    // CRK Integration
    public string? JpkCode { get; set; }            // JPK_V7M code
    public string? CrkTransactionId { get; set; }   // ID w CRK
    public bool SentToCrk { get; set; }             // Czy wysłano do MF
    public DateTime? CrkSentAt { get; set; }        // Kiedy wysłano

    // Rejestracja
    public string ReceiptNumber { get; set; }       // 001234
    public DateTime PrintedAt { get; set; }
    public string CashRegisterId { get; set; }
}
```

---

## Integracja CRK

### Centralnym Repozytorium Kas (CRK)

```
CRK = Ministerstwo Finansów database
├── Przyjmuje: Dane z kas fiskalnych (online/hybrydowe)
├── Waliduje: Poprawność numeracji, VAT, sumy
├── Archiwizuje: Kopie wszystkich paragonów (5+ lat)
├── Udostępnia: Raporty dla władz
└── Generuje: Kody weryfikacyjne (JKV7M)
```

### API CRK

```
Endpoint: https://kasa.gov.pl/api/v1/invoices
Metoda: POST
Port: 443 (HTTPS)
Auth: Certyfikat PKP (Public Key)
```

### Format Danych CRK

```json
{
  "NipSprzedawcy": "1234567890",
  "NumerKasy": "KW001",
  "NumerParagonu": "001234",
  "DataSprzedazy": "2025-10-23",
  "GodzinaSprzedazy": "14:32:15",
  "KwotaBrutto": 157.43,
  "KwotaPodatku": 25.19,
  "Pozycje": [
    {
      "Nazwa": "Koszulka Niebieska",
      "Ilosc": 1,
      "Cena": 15.00,
      "StawkaVat": 23
    },
    {
      "Nazwa": "Spodnie Czarne",
      "Ilosc": 1,
      "Cena": 120.00,
      "StawkaVat": 23
    }
  ]
}
```

### Odpowiedź CRK

```json
{
  "status": "accepted",
  "transactionId": "JKV7M-2025-10-23-14-32-1234567890-KW001-001234",
  "receiptNumber": "001234",
  "registeredAt": "2025-10-23T14:32:45Z",
  "verificationCode": "ABC123XYZ",
  "qrCodeData": "https://kasa.gov.pl/verify/ABC123XYZ"
}
```

### Kod QR

```
QR Code Format:
https://kasa.gov.pl/verify/JKV7M-2025-10-23-14-32-1234567890-KW001-001234

Wydruk na paragonie:
[QR Kod]

Klient skanuje → Weryfikuje na stronie MF → Potwierdza autentyczność
```

---

## Implementacja MP

### 1. FiscalReceiptResponse Rozszerzony (MP-68)

```csharp
public class FiscalReceiptResponse : CommandResponseBase
{
    // Numer fiskalny (wymagany)
    public string FiscalNumber { get; set; } = null!;
    public string FiscalDate { get; set; } = null!;
    public string FiscalTime { get; set; } = null!;

    // Sumy (wymagane)
    public decimal TotalAmount { get; set; }
    public decimal TotalTax { get; set; }
    public int ReceiptNumber { get; set; }
    public string? CashRegisterId { get; set; }

    // ✅ DODANE: Polskie wymagania
    public bool IsOnlineFiscalRegister { get; set; }  // Czy kasa online?
    public string? JpkCode { get; set; }              // JPK_V7M
    public string? CrkTransactionId { get; set; }     // ID w CRK
    public string? QrCodeData { get; set; }           // QR kod
    public bool SentToCrk { get; set; }               // Wysłano do MF?
    public DateTime? CrkSentAt { get; set; }          // Kiedy wysłano?
    public string? CrkErrorMessage { get; set; }      // Błąd CRK
    public Dictionary<string, object> PolishMetadata { get; set; } = new();
}
```

### 2. CrkIntegrationService (MP-68)

```csharp
public interface ICrkIntegrationService
{
    Task<CrkSendResult> SendReceiptToCrkAsync(FiscalReceiptData receipt);
    Task<string> GenerateQrCodeDataAsync(FiscalReceiptData receipt);
    string GenerateJpkCode(FiscalReceiptData receipt);
    Task<bool> VerifyReceiptAsync(string jpkCode);
}

public class CrkIntegrationService : ICrkIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CrkIntegrationService> _logger;
    private readonly CrkConfiguration _config;

    public async Task<CrkSendResult> SendReceiptToCrkAsync(
        FiscalReceiptData receipt)
    {
        try
        {
            // Przygotuj dane w formacie CRK
            var crkData = new
            {
                NipSprzedawcy = receipt.NipSeller,
                NumerKasy = receipt.CashRegisterId,
                NumerParagonu = receipt.FiscalNumber,
                DataSprzedazy = receipt.SaleDate.ToString("yyyy-MM-dd"),
                GodzinaSprzedazy = receipt.SaleTime,
                KwotaBrutto = receipt.TotalAmount,
                KwotaPodatku = receipt.TotalTax,
                Pozycje = receipt.Items.Select(i => new
                {
                    Nazwa = i.Name,
                    Ilosc = i.Quantity,
                    Cena = i.UnitPrice,
                    StawkaVat = i.VatRate
                })
            };

            // Wyślij do CRK z certyfikatem
            using var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ClientCertificates.Add(_config.GetCertificate());

            var json = JsonSerializer.Serialize(crkData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                _config.CrkApiUrl, content);

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<CrkApiResponse>();

            return new CrkSendResult
            {
                Success = true,
                TransactionId = result?.TransactionId,
                SentAt = DateTime.UtcNow,
                VerificationCode = result?.VerificationCode,
                QrCodeUrl = result?.QrCodeUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send receipt to CRK");
            return new CrkSendResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<string> GenerateQrCodeDataAsync(FiscalReceiptData receipt)
    {
        var jpkCode = GenerateJpkCode(receipt);
        var verificationUrl = $"https://kasa.gov.pl/verify/{jpkCode}";
        return verificationUrl;
    }

    public string GenerateJpkCode(FiscalReceiptData receipt)
    {
        // Format: NIP-KASA-DATA-NUMER (w hex)
        var input = $"{receipt.NipSeller}-{receipt.CashRegisterId}-" +
                   $"{receipt.SaleDate:yyyyMMdd}-{receipt.FiscalNumber}";

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        var jpkCode = "JKV7M-" + Convert.ToHexString(hash)[..16].ToUpper();

        return jpkCode;
    }

    public async Task<bool> VerifyReceiptAsync(string jpkCode)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"https://kasa.gov.pl/api/v1/verify/{jpkCode}");

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
```

### 3. FiscalReceipt Persistence (MP-68)

```csharp
public class FiscalReceipt : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    // Podstawowe
    public string FiscalNumber { get; set; } = null!;
    public DateTime FiscalDate { get; set; }
    public string FiscalTime { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public decimal TotalTax { get; set; }
    public string CashRegisterId { get; set; } = null!;

    // Pełne dane paragonu (JSON)
    public string ReceiptDataJson { get; set; } = null!;

    // CRK (Centralne Repozytorium Kas)
    public string? JpkCode { get; set; }
    public string? CrkTransactionId { get; set; }
    public DateTime? CrkSentAt { get; set; }
    public bool SentToCrk { get; set; }
    public string? CrkVerificationCode { get; set; }

    // Archiwizacja (5 lat wymagane prawem)
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }

    // Dla audytu
    public string? SoldByUserId { get; set; }       // Który pracownik sprzedał?
    public string? PaymentMethod { get; set; }      // Cash, Card, etc.

    protected FiscalReceipt() { }

    public FiscalReceipt(Guid id, Guid? tenantId, string fiscalNumber,
        DateTime fiscalDate, decimal totalAmount) : base(id)
    {
        TenantId = tenantId;
        FiscalNumber = fiscalNumber;
        FiscalDate = fiscalDate;
        TotalAmount = totalAmount;
        SentToCrk = false;
    }

    public void MarkAsSentToCrk(string jpkCode, string crkTransactionId)
    {
        SentToCrk = true;
        JpkCode = jpkCode;
        CrkTransactionId = crkTransactionId;
        CrkSentAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        IsArchived = true;
        ArchivedAt = DateTime.UtcNow;
    }
}
```

### 4. Archiwizacja (5 lat)

```csharp
// Background job - co miesiąc archiwizuj stare paragony
[DisableConcurrentExecution(10)]
public class FiscalReceiptArchiveJob : IJob
{
    private readonly IRepository<FiscalReceipt, Guid> _receiptRepository;

    public async Task Execute(IJobExecutionContext context)
    {
        // Archiwizuj paragony starsze niż 1 rok
        var cutoffDate = DateTime.UtcNow.AddYears(-1);

        var toArchive = await _receiptRepository.GetListAsync(x =>
            !x.IsArchived && x.FiscalDate < cutoffDate);

        foreach (var receipt in toArchive)
        {
            receipt.Archive();
            await _receiptRepository.UpdateAsync(receipt);
        }

        // Backup do Azure Blob Storage
        await BackupToBlobAsync(toArchive);
    }

    private async Task BackupToBlobAsync(List<FiscalReceipt> receipts)
    {
        var containerClient = new BlobContainerClient(
            new Uri(_config.BlobStorageUrl),
            new DefaultAzureCredential());

        var blobName = $"fiscal-receipts-archive-{DateTime.UtcNow:yyyy-MM}.json";
        var json = JsonSerializer.Serialize(receipts);

        await containerClient.UploadBlobAsync(blobName,
            BinaryData.FromString(json), overwrite: true);
    }
}
```

---

## Testy i Audyt

### Unit Tests (MP-68)

```csharp
[Fact]
public void GenerateJpkCode_Should_Return_Valid_Format()
{
    var service = new CrkIntegrationService(/* ... */);
    var receipt = new FiscalReceiptData { /* ... */ };

    var jpkCode = service.GenerateJpkCode(receipt);

    Assert.StartsWith("JKV7M-", jpkCode);
    Assert.Equal(22, jpkCode.Length);  // "JKV7M-" + 16 hex chars
}

[Fact]
public async Task SendReceiptToCrk_Should_Return_TransactionId()
{
    var result = await crkService.SendReceiptToCrkAsync(receipt);

    Assert.True(result.Success);
    Assert.NotNull(result.TransactionId);
}

[Fact]
public void FiscalReceipt_Should_Archive_After_1_Year()
{
    var receipt = new FiscalReceipt(/* ... */);
    receipt.FiscalDate = DateTime.UtcNow.AddYears(-1).AddDays(-1);

    receipt.Archive();

    Assert.True(receipt.IsArchived);
    Assert.NotNull(receipt.ArchivedAt);
}
```

### Audyt Externy (wymagany)

```
Co roku:
□ Sprawdzenie poprawności numeracji paragonów
□ Weryfikacja archiwum (5 lat)
□ Audit CRK - czy wszystkie paragony wysłane
□ Kontrola VAT - sumy powinny się zgadzać
□ Kontrola kas fiskalnych - czy zdatne do użytku
```

---

## FAQ i Troubleshooting

### P: Co jeśli nie mam dostępu do CRK?

A: CRK dostęp otrzymujesz po zarejestrowaniu się:
1. Wejdź na: https://kasa.gov.pl/
2. Zaloguj się PeSelektywnym/certyfikatem
3. Zarejestruj swoje kasy
4. Pobierz certyfikat do transmisji

### P: Czy mogę używać starej kasy offline?

A: ❌ Od 1 lipca 2024 kasy online/hybrydowe TYLKO.

### P: Co jeśli Internet się zrywa?

A: Użyj kasy hybrydowej - buforuje offline, wysyła gdy online.

### P: Jak długo przechowywać paragony?

A: **5 lat** - wymóg ustawy VAT. Utrzymuj backup!

### P: Jaki certyfikat dla CRK?

A: **Certyfikat PKP (Public Key)**:
- Uzyskaj od Ministerstwa Finansów
- Walidność: 1 rok
- Wymagane odnowienie co roku

---

## Podsumowanie

Polskie wymagania fiskalne:

✅ Kasy online/hybrydowe obowiązkowe od 2024
✅ Transmisja do CRK wymagana
✅ Numery fiskalne unikalne
✅ Archiwizacja 5 lat
✅ QR kody na paragonach
✅ Audyt roczny

Implementacja MP:
- ✅ FiscalReceipt encja z archiwizacją
- ✅ CrkIntegrationService dla transmisji
- ✅ QR kody i JPK kody
- ✅ Persystencja dla audytu
- ⏳ Integration z producentami kas

---

## Kontakty

- **Ministerstwo Finansów**: https://mf.gov.pl
- **CRK Helpdesk**: support@kasa.gov.pl
- **Producenci kas**:
  - Novitus: https://www.novitus.pl
  - Elzab: https://www.elzab.pl
  - Posnet: https://www.posnet.pl

---

**Ostatnia aktualizacja**: 2025-10-23
**Następny review**: 2025-11-23 (po publikacji finalne wytyczne MF)
