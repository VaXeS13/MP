# LocalAgent Setup & Configuration Guide

Instrukcja instalacji i konfiguracji MP.LocalAgent na komputerze kasiera z integracją kasy fiskalnej, terminala płatniczego i API.

## 📋 Spis Treści

1. [Wymagania](#wymagania)
2. [Instalacja Agenta](#instalacja-agenta)
3. [Rejestracja w API](#rejestracja-w-api)
4. [Konfiguracja Agenta](#konfiguracja-agenta)
5. [Konfiguracja Urządzeń](#konfiguracja-urządzeń)
6. [Testowanie Integracji](#testowanie-integracji)
7. [Troubleshooting](#troubleshooting)

---

## Wymagania

### System Operacyjny
- Windows 10/11 lub Windows Server 2016+
- Administrator account dla instalacji jako Windows Service
- .NET 9.0 Runtime lub Runtime Hosting Bundle

### Sprzęt
- **Terminal Płatniczy**: np. Ingenico, PAX, Verifone (obsługiwane przez API producenta)
- **Kasa Fiskalna**: Niezbędna dla Polski - obsługiwana przez LocalAgent
- **Sieć**: Połączenie internetowe do Azure API (stabilne, IP statyczne rekomendowane)

### Oprogramowanie
- .NET 9.0 Runtime: https://dotnet.microsoft.com/download/dotnet/9.0
- (Opcjonalnie) Visual Studio Code lub Notepad++ do edycji JSON

---

## Instalacja Agenta

### Krok 1: Pobranie Plików Agenta

```bash
# Na komputerze kasiera (w folderu C:\MP\LocalAgent)
cd C:\MP
# Pobierz release build - będzie to C:\MP\LocalAgent\bin\Release\net9.0\

# Lub zbuduj samodzielnie:
cd C:\Users\vaxes\source\repos\MP\MP
dotnet publish src/MP.LocalAgent/MP.LocalAgent.csproj -c Release -o C:\MP\LocalAgent\Release
```

**Struktura folderów po budowie:**
```
C:\MP\LocalAgent\Release\
├── MP.LocalAgent.exe
├── appsettings.json
├── appsettings.Production.json (jeśli istnieje)
├── MP.LocalAgent.dll
├── *.deps.json
└── Logs/
```

### Krok 2: Weryfikacja .NET 9.0

```bash
# W PowerShell (jako Administrator)
dotnet --version
# Powinien pokazać: 9.0.x lub wyżej

# Jeśli brakuje, zainstaluj:
# Pobierz z: https://dotnet.microsoft.com/download/dotnet/9.0
# Wybierz: .NET Runtime lub .NET Hosting Bundle (lepiej dla Windows Service)
```

### Krok 3: Test Uruchomienia (Console Mode)

```bash
# Na komputerze kasiera
cd C:\MP\LocalAgent\Release

# Uruchom w konsoli aby sprawdzić konfigurację
.\MP.LocalAgent.exe

# Powinno pojawić się:
# [INF] Starting MP Local Agent...
# [INF] Initializing MP Local Agent services...
# [INF] Offline command store initialized
# [INF] MP Local Agent services initialized successfully

# Aby zatrzymać: Ctrl+C
```

Jeśli widać błędy - sprawdź sekcję [Troubleshooting](#troubleshooting).

---

## Rejestracja w API

### Krok 1: Login do Systemu MP (Azure)

1. Otwórz https://localhost:44377 (lub productiondomain.com)
2. Login jako Administrator
3. Przejdź do **Settings** → **Agent Management** (lub podobnie)

### Krok 2: Rejestracja Nowego Agenta

W Admin Panel:

1. **Agent Name**: `DEVICE_NAME_KASIER_1` (np. `KASA_WARSZAWA_01`)
2. **Agent Type**: `LocalAgent` lub `CashierTerminal`
3. **Location**: Warszawa (lub lokalizacja faktyczna)
4. **Device Type**:
   - ✅ Terminal Płatniczy
   - ✅ Kasa Fiskalna
   - ✅ Barcode Scanner (opcjonalnie)

**Kliknij: Register Agent**

Otrzymasz:
- **Agent ID**: np. `550e8400-e29b-41d4-a716-446655440000`
- **Tenant ID**: np. `550e8400-e29b-41d4-a716-446655440001`

### Krok 3: Generowanie API Key

W Agent Management:

1. Kliknij na zarejestrowanego agenta
2. **Security** sekcja
3. **Generate New API Key**

Otrzymasz:
```
API Key Prefix: MPA_DEVICE_1234_
API Key (SECRET - skopiuj teraz!): MPA_DEVICE_1234_abcdef1234567890abcdef1234567890
```

⚠️ **WAŻNE**: Skopiuj pełny klucz! Następnym razem nie będzie widoczny!

### Krok 4: IP Whitelisting (bezpieczeństwo)

Opcjonalnie w Agent Security:

1. **IP Whitelist** → Add IP
2. Wpisz IP komputera kasiera (np. `192.168.1.100` lub `*` dla testu)
3. **Save**

---

## Konfiguracja Agenta

### Krok 1: Edytuj `appsettings.json`

Na komputerze kasiera (`C:\MP\LocalAgent\Release\appsettings.json`):

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/localagent-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  },
  "LocalAgent": {
    "TenantId": "550e8400-e29b-41d4-a716-446655440001",
    "AgentId": "550e8400-e29b-41d4-a716-446655440000",
    "ServerUrl": "https://localhost:44377",
    "ApiKey": "MPA_DEVICE_1234_abcdef1234567890abcdef1234567890",
    "HeartbeatIntervalSeconds": 30,
    "ReconnectTimeoutSeconds": 120,
    "MaxCommandQueueSize": 10000
  },
  "Devices": {
    "Terminal": {
      "ProviderId": "mock",
      "Enabled": true,
      "ConnectionString": "COM3",
      "BaudRate": 9600,
      "Model": "Ingenico_iCT250",
      "Timeout": 30
    },
    "FiscalPrinter": {
      "ProviderId": "mock",
      "Enabled": true,
      "ConnectionString": "\\\\\\\\192.168.1.10\\\\LPR1",
      "Model": "Posnet_Thermal_FV",
      "Timeout": 20
    },
    "BarcodeScanner": {
      "ProviderId": "mock",
      "Enabled": true,
      "ConnectionString": "COM4",
      "BaudRate": 9600
    }
  }
}
```

### Krok 2: Wyjaśnienie Konfiguracji

| Pole | Wartość | Opis |
|------|---------|------|
| `TenantId` | GUID z kroku 2.1 | ID dzierżawcy (np. Warszawa) |
| `AgentId` | GUID z kroku 2.1 | Unikalny ID agenta |
| `ServerUrl` | URL API | `https://localhost:44377` (dev) lub domena produkcyjna |
| `ApiKey` | Klucz z kroku 2.3 | Uwierzytelnienie wobec API |
| `HeartbeatIntervalSeconds` | 30 | Jak często wysyłać heartbeat do API |
| `ReconnectTimeoutSeconds` | 120 | Timeout dla reconnect (2 min) |

### Krok 3: Konfiguracja Terminala

W sekcji `Terminal`:

```json
"Terminal": {
  "ProviderId": "real_device",  // zmień z "mock" na rzeczywisty provider
  "Enabled": true,
  "Model": "INGENICO_iCT250",
  "ConnectionString": "COM3",   // Port COM terminala
  "BaudRate": 9600,
  "Timeout": 30
}
```

**Dla Różnych Terminali:**

- **Ingenico iCT250**:
  ```
  "ConnectionString": "COM3"
  "BaudRate": 9600
  ```

- **PAX A80**:
  ```
  "ConnectionString": "COM4"
  "BaudRate": 115200
  ```

- **Verifone Omni 3750**:
  ```
  "ConnectionString": "192.168.1.50:7001"  // IP:Port
  "Protocol": "TCP"
  ```

### Krok 4: Konfiguracja Kasy Fiskalnej

W sekcji `FiscalPrinter`:

```json
"FiscalPrinter": {
  "ProviderId": "real_device",
  "Enabled": true,
  "Model": "POSNET_THERMAL_FV",
  "ConnectionString": "\\\\192.168.1.10\\Drukarka_Fiskalna",
  "Timeout": 20
}
```

**Dla Różnych Kas:**

- **Posnet Thermal FV** (sieciowa):
  ```
  "ConnectionString": "\\\\192.168.1.10\\LPR1"
  ```

- **Elzab Mera** (seria COM):
  ```
  "ConnectionString": "COM5"
  "BaudRate": 19200
  ```

- **Novitus Mała Gwiazda** (USB):
  ```
  "ConnectionString": "USB_DEVICE_ID"
  ```

### Krok 5: Test Konfiguracji

Uruchom agenta w trybie testowym:

```bash
cd C:\MP\LocalAgent\Release

# Ustaw env do testów
$env:DOTNET_ENVIRONMENT = "Development"

# Uruchom
.\MP.LocalAgent.exe

# Sprawdź w logach (Logs/localagent-YYYY-MM-DD.txt):
# [INF] Initializing fiscal printer with provider mock
# [INF] Initializing terminal with provider mock
# [INF] Connecting to SignalR hub at https://localhost:44377
```

---

## Konfiguracja Urządzeń

### Krok 1: Fizyczne Podłączenie Terminala

1. **Urządzenie**: Połącz terminal do komputera kasiera przez:
   - **Port COM** (serial): Kabel RS-232
   - **USB**: Adapter USB-Serial
   - **Ethernet**: Port sieciowy z IP statycznym

2. **Weryfikacja Portu**:
   ```bash
   # W PowerShell
   Get-WmiObject Win32_SerialPort | Select Name, Description
   # Powinno pokazać np: COM3 - USB Serial Port
   ```

3. **Ustawienia Terminala** (na urządzeniu):
   - Baud Rate: 9600 lub 115200 (sprawdź manual)
   - Data Bits: 8
   - Stop Bits: 1
   - Parity: None

### Krok 2: Fizyczne Podłączenie Kasy Fiskalnej

1. **Urządzenie**: Połącz kasę do:
   - **Sieci (LPR)**: IP-adres kasy + port (domyślnie 9100)
   - **Port COM**: Kabel RS-232
   - **USB**: Adapter USB-Serial

2. **Znalezienie IP Kasy**:
   ```bash
   # W PowerShell
   arp -a
   # Poszukaj wpisu Posnet/Elzab/Novitus

   # Lub sprawdź w ustawieniach kasy (Menu → Sieć → Status)
   ```

3. **Test Połączenia do Kasy (sieciowej)**:
   ```bash
   ping 192.168.1.10
   # Powinno: Reply from 192.168.1.10: bytes=32 time=5ms TTL=64
   ```

### Krok 3: Rejestracja Urządzeń w API

W Admin Panel → Agent Management → Agent Details:

1. **Terminal**:
   - Status: `Online` / `Offline`
   - Model: `INGENICO_iCT250`
   - Serial: (automatycznie pobrane z urządzenia)
   - Firmware: (automatycznie pobrane)

2. **Kasa Fiskalna**:
   - Status: `Online` / `Offline`
   - Model: `POSNET_THERMAL_FV`
   - Serial: (automatycznie pobrane)
   - Last Z-Report: (bieżąca data)

### Krok 4: Konfiguracja Obsługi Urządzeń

W sekcji `MP.Application/ItemCheckoutAppService.cs`:

Urządzenia są już skonfigurowane do automatycznego użytku poprzez `IRemoteDeviceProxy`:

```csharp
// Nie musisz nic robić - jest automatyczne!
// Proxy będzie automatycznie:
// 1. Autoryzować płatność na terminalu
// 2. Drukować paragon na kasie
// 3. Śledzić transakcje w CRK
```

---

## Testowanie Integracji

### Test 1: Weryfikacja Połączenia SignalR

1. Uruchom agenta:
   ```bash
   cd C:\MP\LocalAgent\Release
   .\MP.LocalAgent.exe
   ```

2. W logach powinno być:
   ```
   [INF] Connecting to SignalR hub at https://localhost:44377
   [INF] Connected to Azure API
   [INF] Agent registered: 550e8400-e29b-41d4-a716-446655440000
   ```

3. Jeśli brak lub błąd - sprawdź:
   - Czy API jest uruchomiony: `https://localhost:44377/health-status`
   - Czy API Key jest poprawny
   - Czy IP agenta jest na whitelist

### Test 2: Weryfikacja Terminala

W Admin Panel → Device Status:

```
Terminal: INGENICO_iCT250
Status: ✅ Online
Last Heartbeat: 30s ago
Capabilities: [VISA, Mastercard, Contactless, EMV, 3D-Secure]
```

Lub test ręczny:
```bash
# W PowerShell na komputerze kasiera
# Wysyłaj test do terminala

# Terminal powinien odpowiedzieć: "READY" lub "OK"
```

### Test 3: Weryfikacja Kasy Fiskalnej

W Admin Panel → Device Status:

```
Fiscal Printer: POSNET_THERMAL_FV
Status: ✅ Online
Last Receipt: 2024-10-23 14:32:00
Total Receipts Today: 45
Paper Status: ✅ OK
Fiscal Memory: 23% used
```

Test ręczny:
```bash
# Wydrukuj test receipt z API
POST https://localhost:44377/api/app/fiscal/test-receipt
Content-Type: application/json

{
  "agentId": "550e8400-e29b-41d4-a716-446655440000",
  "amount": 100.00,
  "description": "Test Receipt"
}

# Kasa powinna wydrukować paragon testowy
```

### Test 4: Integracja Pełnego Przepływu Sprzedażowego

1. **Otwórz punkt sprzedaży** (ng serve lub production)
2. **Dodaj buty do koszyka**
3. **Przejdź do kasy**
4. **Wybierz płatność kartą**
5. **System:**
   - ✅ Wyśle żądanie do Terminala
   - ✅ Terminal zażąda karty
   - ✅ Zarejestruje autoryzację
   - ✅ Wydrukuje paragon na kasie
   - ✅ Zarejestruje w CRK
   - ✅ Pokażę potwierdzenie

```json
// Spodziewana odpowiedź
{
  "status": "SUCCESS",
  "transactionId": "TRX_20241023_001",
  "terminalId": "INGENICO_iCT250",
  "amount": 250.50,
  "currency": "PLN",
  "receiptNumber": "00001234",
  "timestamp": "2024-10-23T14:35:22.123Z",
  "crk_registered": true,
  "masked_pan": "****1234"
}
```

### Test 5: Offline Mode

1. **Rozłącz Internet** (lub wyłącz API)
2. **Wykonaj płatność**
3. System powinien:
   - ✅ Pokazać "Offline Mode"
   - ✅ Zapisać komendę do SQLite
   - ✅ Wysłać po powrocie do sieci

Sprawdzenie SQLite:
```bash
# Na komputerze kasiera
cd C:\Users\[User]\AppData\Local\MP\LocalAgent

# Lista offline komend
sqlite3 commands.db
> SELECT CommandId, Status FROM OfflineCommands LIMIT 5;
```

---

## Troubleshooting

### Problem 1: Agent nie łączy się do API

**Symptomy**:
```
[ERR] Failed to connect to SignalR hub
[ERR] Connection timeout after 120 seconds
```

**Rozwiązanie**:
```bash
# 1. Sprawdź czy API jest dostępny
curl https://localhost:44377/health-status
# Powinien zwrócić 200 OK

# 2. Sprawdź czy Agent.ApiKey jest poprawny
# W appsettings.json porównaj z API Panel

# 3. Sprawdź firewall
# Port 44377 musi być dostępny (lub port produkcyjny)

# 4. Sprawdź DNS (jeśli domena)
nslookup api.yourdomain.com
```

### Problem 2: Terminal offline

**Symptomy**:
```
[WRN] Terminal status: OFFLINE
[ERR] Payment command timeout after 30 seconds
```

**Rozwiązanie**:
```bash
# 1. Sprawdź fizyczne połączenie
# - Czy kabel COM/USB jest podłączony?
# - Czy port w Deviceu Manager pokazuje COM3/COM4?

# 2. Sprawdź port w appsettings.json
"ConnectionString": "COM3"  # czy to prawidłowy port?

# 3. Test połączenia z terminalem
# W PowerShell
$port = New-Object System.IO.Ports.SerialPort COM3
$port.Open()
$port.WriteLine("AT")  # test polecenia
$port.Close()

# 4. Sprawdź baud rate (domyślnie 9600)
# W manual terminala
```

### Problem 3: Kasa nie drukuje

**Symptomy**:
```
[ERR] Fiscal printer error: OFFLINE
[ERR] Paper out or printer not ready
```

**Rozwiązanie**:
```bash
# 1. Sprawdź papier w kasie
# Otwórz kase i załaduj papier

# 2. Sprawdź połączenie sieciowe (jeśli sieciowa)
ping 192.168.1.10
# Powinien zwrócić odpowiedź

# 3. Sprawdź status kasy
# Na ekranie kasy: Menu → Status
# Powinno pokazywać: "Ready" lub "Online"

# 4. Test manualny
# Z interfejsu kasy: Print Test Receipt
```

### Problem 4: API Key rejected

**Symptomy**:
```
[ERR] Authentication failed: Invalid API Key
[ERR] Code 401: Unauthorized
```

**Rozwiązanie**:
```bash
# 1. Sprawdź czy API Key jest wklejony poprawnie
# W appsettings.json - nie może być: spacji, złamania linii

# 2. Sprawdź czy jest aktywny w API Panel
# Agent Management → Security → API Keys
# Status powinien być: ✅ Active

# 3. Sprawdź czy nie jest wygasły
# Expiration Date powinien być w przyszłości

# 4. Sprawdź IP Whitelist
# Jeśli ustawiony - IP komputera musi być na liście
```

### Problem 5: SQLite database locked

**Symptomy**:
```
[ERR] SQLite database is locked
[ERR] Offline commands cannot be saved
```

**Rozwiązanie**:
```bash
# 1. Sprawdź czy agent jest uruchomiony 2x
# W Task Manager: Powinno być 1x MP.LocalAgent.exe

# 2. Zrestartuj agenta
# Ctrl+C w konsoli
# Czekaj 5 sekund
# Uruchom ponownie

# 3. Usuń lock file (jeśli istnieje)
cd C:\Users\[User]\AppData\Local\MP\LocalAgent
# Szukaj: commands.db-shm, commands.db-wal
# Usuń je i zrestartuj
```

### Problem 6: Dysk pełny - zbyt wiele offline komend

**Symptomy**:
```
[ERR] Queue size exceeded: 10000/10000 commands
[ERR] Cannot save new commands
```

**Rozwiązanie**:
```bash
# 1. Sprawdź czy API jest dostępny
# Agent powinien wysłać offline komendy

# 2. Wyczyść stare komendy (>7 dni)
# Agent robi to automatycznie co 5 minut
# Ale możesz ręcznie:

sqlite3 commands.db
> DELETE FROM OfflineCommands WHERE CreatedAt < datetime('now', '-7 days');
> VACUUM;
> .quit

# 3. Zrestartuj agenta
```

---

## Instalacja jako Windows Service

Po testowaniu w konsoli, zainstaluj jako Windows Service:

```bash
# W PowerShell (jako Administrator)

# 1. Zatrzymaj proces jeśli jest uruchomiony
Stop-Process -Name "MP.LocalAgent" -Force

# 2. Instalacja jako service
$ServiceName = "MPLocalAgent"
$ServicePath = "C:\MP\LocalAgent\Release\MP.LocalAgent.exe"

New-Service `
  -Name $ServiceName `
  -BinaryPathName $ServicePath `
  -DisplayName "MP Local Agent" `
  -Description "Local agent for MP POS System - manages fiscal devices" `
  -StartupType Automatic

# 3. Uruchom service
Start-Service -Name $ServiceName

# 4. Sprawdź status
Get-Service -Name $ServiceName
# Powinno być: Status = Running

# 5. Sprawdź logi
Get-EventLog -LogName Application -Source MPLocalAgent -Newest 5
```

### Usunięcie Service (jeśli potrzeba)

```bash
# PowerShell (Administrator)
Stop-Service -Name "MPLocalAgent"
Remove-Service -Name "MPLocalAgent"
```

---

## Aktualizacja Agenta

Gdy pojawi się nowa wersja:

```bash
# 1. Zatrzymaj service
Stop-Service -Name "MPLocalAgent"

# 2. Utwórz backup konfiguracji
Copy-Item C:\MP\LocalAgent\Release\appsettings.json `
         C:\MP\LocalAgent\Release\appsettings.json.backup

# 3. Skopiuj nowe pliki
# Pobierz nowy release i skopiuj do: C:\MP\LocalAgent\Release\
# ZACHOWAJ appsettings.json (konfiguracja)

# 4. Uruchom service
Start-Service -Name "MPLocalAgent"

# 5. Sprawdź logi
tail -f C:\Users\[User]\AppData\Local\MP\LocalAgent\Logs\localagent-*.txt
```

---

## Monitorowanie i Utrzymanie

### Daily Checklist

```bash
# Sprawdzaj co 24h:

# 1. Status Z-Report (kasa fiskalna)
# Powinien być generowany raz dziennie o zamknięciu
curl https://localhost:44377/api/app/fiscal/z-report

# 2. Heartbeat agent'a
# Powinien być co 30 sekund
# Logach: [INF] Heartbeat sent to Azure

# 3. Papier w kasie
# Sprawdź fizycznie i w Status

# 4. Rozmiar SQLite database
# Nie powinien rosnąć (offset commands są czyszczone)

# 5. Logi błędów
# Sprawdź Logs/ folder - nie powinno być ERR
```

### Monthly Tasks

- Backupuj `appsettings.json`
- Sprawdzaj CRK reconciliation (czy sumy się zgadzają)
- Testuj offline mode
- Aktualizuj agent do najnowszej wersji

---

## Wsparcie i Dokumentacja

- **API Documentation**: https://localhost:44377/swagger
- **Agent Status Panel**: https://localhost:44377/admin/agents
- **Fiscal Compliance**: Patrz sekcja CRK w CLAUDE.md
- **Security Keys**: MP-67 Agent Authentication w CLAUDE.md
- **Local Logs**: `C:\Users\[User]\AppData\Local\MP\LocalAgent\Logs\`
