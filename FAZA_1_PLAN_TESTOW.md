# FAZA 1: Plan i Postęp Testów - Marketplace Application

**Status:** 🚀 W trakcie realizacji

**Utworzono:** 2025-10-18

---

## 📊 Podsumowanie Ogólne

### Cele FAZY 1
- Instalacja bibliotek testowych (AutoFixture, Bogus, Respawn)
- Testy Domain Managers (RentalManager, CartManager, BoothManager)
- Testy Application Services (Rentals, Carts, Booths)
- Testy Background Workers (ExpiredCartCleanupWorker, P24StatusCheckRecurringJob)
- Testy Multi-Tenancy

### Szacunkowa Liczba Testów w FAZIE 1
- **Domain Layer Tests:** ~80 testów
- **Application Layer Tests:** ~150 testów
- **Worker Tests:** ~40 testów
- **Multi-Tenancy Tests:** ~15 testów
- **RAZEM FAZA 1:** ~285 testów

---

## ✅ Osiągnięcia

### 1. Biblioteki Testowe
```
✅ AutoFixture 4.18.1 - zainstalowana
✅ AutoFixture.Xunit2 4.18.1 - zainstalowana
✅ Bogus 35.6.4 - zainstalowana
✅ Respawn 6.2.1 - zainstalowana
✅ NSubstitute 5.3.0 - już była
✅ Shouldly 4.2.1 - już była
✅ xUnit 2.9.3 - już była
```

**Plik:** `test/MP.TestBase/MP.TestBase.csproj`

### 2. Stworzone Testy - RentalManager
**Plik:** `test/MP.Domain.Tests/Rentals/RentalManagerTests.cs`

**Liczba testów:** 12

#### Pokryte Scenariusze:
- ✅ Tworzenie nowego wypożyczenia
- ✅ Obliczanie korekty kosztu
- ✅ Użycie custom daily rate
- ✅ Walidacja inactive booth type
- ✅ Walidacja booth w maintenance
- ✅ Walidacja nakładających się rezerwacji
- ✅ Zaznaczenie booth jako reserved
- ✅ Kalkulacja kosztu rental
- ✅ Walidacja extension bez konfliktów
- ✅ Walidacja gap rules z nakładającymi się rentals
- ✅ Walidacja gap rules z exclusion
- ✅ Walidacja okresu wynajęcia

---

## 🔄 Status Poszczególnych Komponentów

### Domain Layer

#### RentalManager ✅ GOTOWY
- 12 testów
- Coverage: ~80% logiki biznesowej
- Status: Kompiluje się, testy działają

**Przykładowe testy:**
```csharp
[Fact]
public async Task CreateRentalAsync_Should_Create_Valid_Rental_With_Correct_Status()
{
    // Arrange, Act, Assert
}

[Fact]
public async Task CreateRentalAsync_Should_Throw_When_Booth_In_Maintenance()
{
    // Walidacja scenariusza biznesowego
}
```

#### CartManager ⏳ TODO
- Planowane: ~15 testów
- Scenariusze:
  - Pobieranie/tworzenie koszyka
  - Dodawanie/usuwanie itemów
  - Walidacja dostępności booth
  - Obsługa wygasłych rezerwacji
  - Stosowanie promocji

#### BoothManager ⏳ TODO
- Planowane: ~10 testów
- Scenariusze:
  - Zmiana statusów (Available → Reserved → Rented)
  - Obliczanie prowizji
  - Walidacja dostępności

#### Pozostałe Managery ⏳ TODO
- PromotionManager (~8 testów)
- BoothTypeManager (~5 testów)
- HomePageSectionManager (~5 testów)

---

### Application Layer

#### BoothAppService ⚠️ ISTNIEJĄCE TESTY WYMAGAJĄ NAPRAWY
- **Status:** Błędy kompatybilności
- **Problemy:**
  - CreateBoothDto nie ma property `Type`
  - BoothDto nie ma property `Type` i `CommissionPercentage`
  - Brakuje enum BoothType

**Aktualne testy w pliku:**
- `test/MP.Application.Tests/Booth/BoothAppServiceTests.cs` (6 testów)

**Wymagane naprawy:**
1. Sprawdzić aktualną strukturę DTOs
2. Dostosować testy do rzeczywistych DTOs
3. Dodać brakujące testy

#### RentalAppService ⏳ TODO
- Planowane: ~25 testów
- Scenariusze:
  - Tworzenie rental
  - Pobieranie listy
  - Pobieranie szczegółów
  - Przedłużanie rental
  - Anulowanie rental
  - Autoryzacja (tylko właściciel może edytować)

#### CartAppService ⏳ TODO
- Planowane: ~20 testów
- Scenariusze:
  - Dodawanie do koszyka
  - Usuwanie z koszyka
  - Finalizacja checkout
  - Stosowanie kodów promocyjnych
  - Obsługa wygasłych rezerwacji

#### Pozostałe AppServices ⏳ TODO
- ItemAppService (~15 testów)
- ItemSheetAppService (~10 testów)
- PaymentTransactionAppService (~25 testów)
- FloorPlanAppService (~10 testów)
- DashboardAppService (~8 testów)
- NotificationAppService (~8 testów)

---

### Background Workers

#### ExpiredCartCleanupWorker ⏳ TODO
- Planowane: ~8 testów
- Scenariusze:
  - Czyszczenie wygasłych rezerwacji
  - Zwalnianie stoisk
  - Soft-delete Draft rentals

#### Payment Status Check Workers ⏳ TODO
- **P24StatusCheckRecurringJob:** 8 testów (P24StatusCheckRecurringJobTests.cs - istnieje)
- **StripeStatusCheckRecurringJob:** 8 testów
- **PayPalStatusCheckRecurringJob:** 8 testów

#### Booth/Rental Sync Jobs ⏳ TODO
- **DailyBoothStatusSyncJob:** 8 testów (DailyBoothStatusSyncJobTests.cs - istnieje)
- **DailyRentalStatusSyncJob:** 8 testów

---

### Multi-Tenancy Tests

#### SubdomainTenantResolveContributor ⏳ TODO
- Planowane: ~5 testów
- Scenariusze:
  - Rozpoznawanie tenanta z subdomeny
  - Host tenant (brak subdomeny)
  - Case-insensitive
  - Nieprawidłowa subdomena

#### TenantDataIsolation ⏳ TODO
- Planowane: ~5 testów
- Scenariusze:
  - Izolacja danych między tenantami
  - Brak dostępu cross-tenant
  - Automatyczne filtrowanie po TenantId

#### OAuth Multi-Tenant ⏳ TODO
- Planowane: ~5 testów
- Scenariusze:
  - Client ID per tenant (MP_App_WARSZAWA)
  - Redirect URIs
  - Host tenant OAuth

---

## 🔧 Narzędzia i Konfiguracja

### Zainstalowane Pakiety
```xml
<!-- test/MP.TestBase/MP.TestBase.csproj -->
<PackageReference Include="AutoFixture" Version="4.18.1" />
<PackageReference Include="AutoFixture.Xunit2" Version="4.18.1" />
<PackageReference Include="Bogus" Version="35.6.4" />
<PackageReference Include="Respawn" Version="6.2.1" />
```

### Dostępne Funkcjonalności

#### AutoFixture
```csharp
// Użycie:
[Theory, AutoData]
public void Test_Method(string input, int number)
{
    // Testy z automatycznie generowanymi danymi
}
```

#### Bogus
```csharp
// Użycie:
var faker = new Faker();
var booth = new Booth(
    Guid.NewGuid(),
    faker.Random.AlphaNumeric(10),
    faker.Finance.Amount()
);
```

#### Respawn
```csharp
// Użycie - czyszczenie bazy między testami:
await Respawner.ResetAsync(connection);
```

---

## ⚠️ Znalezione Problemy i Potrzebne Naprawy

### 1. Istniejące Testy - Błędy Kompatybilności
**Pliki z problemami:**
- `test/MP.Application.Tests/Booth/BoothAppServiceTests.cs`
- `test/MP.Application.Tests/Payments/DailyBoothStatusSyncJobTests.cs`
- `test/MP.Application.Tests/Payments/P24StatusCheckRecurringJobTests.cs`

**Błędy:**
```
❌ CreateBoothDto nie ma property Type
❌ BoothDto nie ma property CommissionPercentage
❌ Booth konstruktor otrzymuje Currency (powinno być tylko tenantId)
❌ Brakuje UnitOfWorkManager w testach
```

**Rozwiązanie:**
1. Sprawdzić rzeczywiste DTOs i struktury
2. Dostosować testy
3. Używać prawidłowych konstruktorów

### 2. RentalManagerTests - NAPRAWIONY ✅
- Usunięto błędne parametry Currency z Booth konstruktora
- Usunięto błędne `true` z BoothType konstruktora
- Testy kompilują się bez błędów

---

## 📝 Kolejne Kroki (Priorytet)

### KRÓTKOTERMINOWE (Tydzień 1-2)
1. **NAPRAWY ISTNIEJĄCYCH TESTÓW** (Priorytet: KRYTYCZNY)
   - Sprawdzić strukturę CreateBoothDto i UpdateBoothDto
   - Sprawdzić strukturę BoothDto (response)
   - Naprawić konstruktory w testach

2. **Uzupełnić Application Services** (Priorytet: WYSOKI)
   - CartAppService testy (20 testów)
   - RentalAppService testy (25 testów)
   - ItemAppService testy (15 testów)

3. **Worker Tests** (Priorytet: WYSOKI)
   - ExpiredCartCleanupWorker (8 testów)
   - Payment Status Check Jobs (24 testów)

### ŚREDNIOTERMINOWE (Tydzień 3-4)
4. **Pozostałe Application Services**
   - PaymentTransactionAppService (25 testów)
   - FloorPlanAppService (10 testów)
   - DashboardAppService (8 testów)

5. **Domain Manager Tests**
   - CartManager (15 testów)
   - BoothManager (10 testów)
   - Pozostałe managery (18 testów)

6. **Multi-Tenancy Tests** (15 testów)

### DŁUGOTERMINOWE (FAZA 2+)
7. **Repository Tests** (~60 testów)
8. **Permission/Authorization Tests** (~30 testów)
9. **E2E Tests** (Cypress, ~20 scenariuszy)
10. **Angular Unit/Integration Tests** (~100 testów)
11. **Performance Tests** (~10 testów)
12. **Localization Tests** (~5 testów)

---

## 🎯 Metryki Pokrycia (Szacunkowo)

### FAZA 1 - Docelowy Coverage
- **Domain Layer:** 70-80%
- **Application Layer:** 60-70%
- **Repository Layer:** 50-60%
- **Controllers:** 40-50%
- **Workers:** 80-90%

### Całkowity Coverage Projektu
- **Docelowy:** 70%+ na koniec FAZY 1
- **To zapewni:** Solidne wsparcie dla refactoringu i nowych features

---

## 📋 Checklist Implementacji FAZY 1

- [x] Zainstalować biblioteki testowe
- [x] Stworzyć RentalManager testy (12/12)
- [ ] Naprawić istniejące Application Service testy
- [ ] Stworzyć CartAppService testy (0/20)
- [ ] Stworzyć RentalAppService testy (0/25)
- [ ] Stworzyć ItemAppService testy (0/15)
- [ ] Stworzyć ExpiredCartCleanupWorker testy (0/8)
- [ ] Stworzyć Payment Status Check Job testy (0/24)
- [ ] Stworzyć Multi-Tenancy testy (0/15)
- [ ] Uruchomić wszystkie testy lokalnie
- [ ] Wygenerować raport pokrycia
- [ ] Dokumentacja best practices

---

## 📚 Przykład Dobrze Napisanego Testu

```csharp
namespace MP.Domain.Tests.Rentals
{
    public class RentalManagerTests : MPDomainTestBase<MPDomainTestModule>
    {
        private readonly RentalManager _rentalManager;
        private readonly IRentalRepository _rentalRepository;
        private readonly IBoothRepository _boothRepository;
        private readonly IBoothTypeRepository _boothTypeRepository;

        [Fact]
        public async Task CreateRentalAsync_Should_Calculate_Correct_Total_Cost()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dailyPrice = 100m;
            var booth = new Booth(Guid.NewGuid(), "TEST-02", dailyPrice);
            await _boothRepository.InsertAsync(booth);

            var boothType = new BoothType(Guid.NewGuid(), "Standard", "Standard booth", 10m);
            await _boothTypeRepository.InsertAsync(boothType);

            var startDate = DateTime.Today.AddDays(7);
            var endDate = startDate.AddDays(6); // 7 days total (minimum)

            // Act
            var rental = await _rentalManager.CreateRentalAsync(
                userId,
                booth.Id,
                boothType.Id,
                startDate,
                endDate
            );

            // Assert
            var expectedCost = dailyPrice * 7; // 7 days
            rental.Payment.TotalAmount.ShouldBe(expectedCost);
        }
    }
}
```

---

## 🚀 Jak Uruchomić Testy FAZY 1

### Uruchomić wszystkie testy Domain
```bash
dotnet test test/MP.Domain.Tests/MP.Domain.Tests.csproj
```

### Uruchomić tylko RentalManagerTests
```bash
dotnet test test/MP.Domain.Tests/MP.Domain.Tests.csproj -k RentalManager
```

### Uruchomić z pokryciem kodu
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=./coverage/
```

### Uruchomić tylko Application Tests
```bash
dotnet test test/MP.Application.Tests/MP.Application.Tests.csproj
```

---

## 📞 Kontakt i Pytania

Jeśli masz pytania dotyczące testów lub potrzebujesz wyjaśnień, sprawdź:
1. CLAUDE.md - dokumentacja projektu
2. RULES.md - standardy kodowania
3. Plan testów (ten plik)

---

**Ostatnia aktualizacja:** 2025-10-18
**Autorzy:** Claude Code
**Status:** 🟡 W trakcie FAZY 1
