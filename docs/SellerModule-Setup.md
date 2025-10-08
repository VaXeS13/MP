# Seller Module - Setup Instructions

## ✅ Co zostało zaimplementowane:

### Backend (.NET 9)
- ✅ `TenantTerminalSettings` - entity dla ustawień terminali
- ✅ `ITerminalPaymentProvider` - interfejs dla dostawców terminali
- ✅ `MockTerminalProvider` - mock provider do testów
- ✅ `TerminalPaymentProviderFactory` - factory pattern
- ✅ `ItemCheckoutAppService` - logika sprzedaży
- ✅ `TerminalSettingsAppService` - zarządzanie ustawieniami terminali
- ✅ Controllers: `SellerController`, `TerminalSettingsController`
- ✅ Migracja bazy danych wykonana

### Frontend (Angular 19)
- ✅ `SellerCheckoutComponent` - komponent punktu sprzedaży
- ✅ `TerminalSettingsComponent` - panel administracyjny
- ✅ Angular proxy services
- ✅ Routing skonfigurowany
- ✅ Menu: "Terminal Payment Providers" w zakładce Administration
- ✅ Menu: "Seller Checkout" w głównym menu

## 🚀 Jak uruchomić:

### 1. Restart aplikacji backend

**WAŻNE:** Musisz zrestartować aplikację backend aby załadować nowe serwisy!

```bash
# Zatrzymaj aplikację jeśli jest uruchomiona (Ctrl+C lub zamknij Visual Studio)

# Uruchom ponownie:
dotnet run --project src/MP.HttpApi.Host/MP.HttpApi.Host.csproj
```

Lub zrestartuj w Visual Studio (F5).

### 2. Uruchom frontend

```bash
cd angular
ng serve
```

### 3. Weryfikacja API

Otwórz Swagger: `https://localhost:44377/swagger`

Sprawdź czy są dostępne endpointy:
- ✅ `POST /api/app/seller/find-by-barcode`
- ✅ `GET /api/app/seller/payment-methods`
- ✅ `POST /api/app/seller/checkout`
- ✅ `GET /api/app/seller/terminal-status`
- ✅ `GET /api/app/terminal-settings/current`
- ✅ `GET /api/app/terminal-settings/providers`
- ✅ `POST /api/app/terminal-settings`
- ✅ `PUT /api/app/terminal-settings/{id}`
- ✅ `DELETE /api/app/terminal-settings/{id}`

## 📝 Konfiguracja Terminala (PIERWSZY KROK!)

1. **Zaloguj się do aplikacji**
   - Otwórz: `http://localhost:4200`
   - Zaloguj się jako admin

2. **Skonfiguruj Terminal**
   - Przejdź do: **Administration → Terminal Payment Providers**
   - Lub bezpośrednio: `http://localhost:4200/terminal-settings`

3. **Utwórz konfigurację:**
   - **Terminal Provider:** Mock Terminal (Development)
   - **Currency:** PLN
   - **Enabled:** ✅ ON
   - **Sandbox Mode:** ✅ ON
   - **Configuration JSON:**
     ```json
     {}
     ```
   - Kliknij **Create**

## 💰 Korzystanie z punktu sprzedaży

1. **Przejdź do Seller Checkout**
   - Kliknij **Seller Checkout** w menu
   - Lub: `http://localhost:4200/seller-checkout`

2. **Dodaj kod kreskowy do przedmiotu** (jeśli jeszcze nie ma):
   ```sql
   UPDATE AppRentalItems
   SET Barcode = '1234567890'
   WHERE Id = 'your-item-guid';

   -- Upewnij się że przedmiot ma cenę
   UPDATE AppRentalItems
   SET ActualPrice = 100.00
   WHERE Id = 'your-item-guid';
   ```

3. **Skanuj i sprzedaj:**
   - Wpisz lub zeskanuj kod: `1234567890`
   - Kliknij **Find**
   - Wybierz metodę płatności: **Cash** lub **Card**
   - Potwierdź transakcję

## 🧪 Testowanie Mock Terminal

Mock terminal symuluje rzeczywisty terminal:
- ✅ 95% transakcji kończy się sukcesem
- ❌ 5% transakcji kończy się odmową (symulacja)
- ⏱️ Opóźnienie 1.5 sekundy (symulacja przetwarzania)
- 💳 Generuje fikcyjne dane karty (VISA, ostatnie 4 cyfry)

## 🔧 Rozwiązywanie problemów

### Problem: "404 Not Found" na `/api/app/terminal-settings/providers`

**Rozwiązanie:**
1. Sprawdź czy zrestartowałeś backend
2. Sprawdź czy Swagger pokazuje te endpointy
3. Sprawdź logi aplikacji czy są błędy podczas startu

### Problem: "Terminal provider not configured"

**Rozwiązanie:**
1. Przejdź do Terminal Payment Providers
2. Utwórz konfigurację (patrz sekcja "Konfiguracja Terminala")
3. Upewnij się że "Enabled" jest ON

### Problem: "Item not found" podczas skanowania

**Rozwiązanie:**
```sql
-- Sprawdź czy kod kreskowy istnieje
SELECT * FROM AppRentalItems WHERE Barcode = 'twoj-kod';

-- Jeśli nie istnieje, dodaj:
UPDATE AppRentalItems
SET Barcode = 'twoj-kod'
WHERE Id = 'guid-przedmiotu';
```

### Problem: Menu nie pokazuje "Terminal Payment Providers"

**Rozwiązanie:**
1. Wyczyść cache przeglądarki (Ctrl+Shift+R)
2. Przebuduj Angular: `npm run build`
3. Zrestartuj `ng serve`

## 📊 Struktura bazy danych

### Nowa tabela: `AppTenantTerminalSettings`

```sql
SELECT * FROM AppTenantTerminalSettings;
```

Kolumny:
- `Id` - GUID
- `TenantId` - ID tenanta (null = host)
- `ProviderId` - "mock", "ingenico", "stripe_terminal", etc.
- `IsEnabled` - czy włączony
- `ConfigurationJson` - JSON z konfiguracją
- `Currency` - waluta (PLN, EUR, USD)
- `Region` - region/kraj
- `IsSandbox` - tryb sandbox

### Zaktualizowana tabela: `AppRentalItems`

Dodana kolumna:
- `Barcode` - kod kreskowy (VARCHAR 100)

## 🔐 Bezpieczeństwo

⚠️ **Ważne:**
- `ConfigurationJson` przechowuje klucze API
- W produkcji użyj szyfrowania dla wrażliwych danych
- Nie commituj prawdziwych kluczy API do repozytorium
- Dla produkcji: używaj ABP Setting Management lub Azure Key Vault

## 🚀 Następne kroki (opcjonalne)

### Dodanie prawdziwych dostawców terminali:

1. **Stripe Terminal**
   ```csharp
   public class StripeTerminalProvider : ITerminalPaymentProvider
   {
       public string ProviderId => "stripe_terminal";
       // Implementacja...
   }
   ```

2. **Rejestracja w ApplicationModule:**
   ```csharp
   services.AddTransient<StripeTerminalProvider>();
   ```

3. **Konfiguracja w UI:**
   - Provider: Stripe Terminal
   - Configuration JSON:
     ```json
     {
       "apiKey": "sk_test_...",
       "locationId": "tml_..."
     }
     ```

## 📞 Wsparcie

W razie problemów:
1. Sprawdź logi: `src/MP.HttpApi.Host/Logs/`
2. Sprawdź browser console (F12)
3. Sprawdź czy wszystkie serwisy są zarejestrowane w DI

## ✅ Checklist przed pierwszym użyciem:

- [ ] Backend zrestartowany
- [ ] Frontend uruchomiony (`ng serve`)
- [ ] Zalogowany jako admin
- [ ] Terminal skonfigurowany w UI
- [ ] Przynajmniej jeden przedmiot ma kod kreskowy
- [ ] Przedmiot ma ustawioną cenę (`ActualPrice`)
- [ ] Status przedmiotu = "ForSale"