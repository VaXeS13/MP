# Terminal & Fiscal Printer Architecture

## Overview
Multi-provider payment terminal and fiscal printer integration for global POS operations.

## Architecture Layers

### 1. Communication Layer (Protocol Abstraction)
```
ITerminalCommunication
├── TcpIpCommunication (Ingenico Lane series, Verifone VX series)
├── UsbCommunication (USB HID, CDC devices)
├── SerialPortCommunication (RS-232, legacy devices)
├── BluetoothCommunication (mobile terminals)
└── RestApiCommunication (cloud terminals: Stripe, Square, SumUp)
```

### 2. Terminal Provider Layer
```
ITerminalPaymentProvider
├── IngenicoProvider
│   ├── Lane3000Provider
│   ├── Lane5000Provider
│   ├── Lane7000Provider
│   ├── Lane8000Provider
│   ├── Desk5000Provider
│   ├── Move5000Provider (Bluetooth)
│   └── Link2500Provider
├── VerifoneProvider
│   ├── VX520Provider
│   ├── VX680Provider (Wireless)
│   ├── VX820Provider
│   └── P400Provider (Stripe Terminal)
├── PAXProvider
│   ├── A920Provider (Android-based)
│   ├── S920Provider
│   └── IM30Provider (mobile)
├── SumUpProvider (REST API)
├── SquareTerminalProvider (REST API)
├── StripeTerminalProvider (SDK)
└── MockTerminalProvider (Testing)
```

### 3. Fiscal Printer Layer
```
IFiscalPrinterProvider
├── PosnetProvider (Poland)
│   ├── PosnetThermalProvider
│   ├── PosnetErgoProvider
│   └── PosnetBingoProvider
├── ElzabProvider (Poland)
│   ├── ElzabOmegaProvider
│   ├── ElzabSigmaProvider
│   └── ElzabK10Provider
├── NovitusProvider (Poland)
│   ├── NovitusSoleoProvider
│   ├── NovitusNanoProvider
│   └── NovitusDeonProvider
├── EpsonFiscalProvider (Global)
│   ├── EpsonFP81Provider
│   └── EpsonTMT88Provider
├── WincieFiscalProvider (Czech Republic)
├── DatecsFiscalProvider (Bulgaria)
└── MockFiscalPrinterProvider (Testing)
```

## Configuration Architecture

### ABP Settings Structure (Per Tenant)
```json
{
  "MP.Terminal": {
    "Enabled": true,
    "ProviderId": "ingenico_lane_5000",
    "ConnectionType": "tcp_ip",
    "ConnectionSettings": {
      "IpAddress": "192.168.1.100",
      "Port": 8800,
      "Timeout": 30000
    },
    "Currency": "PLN",
    "Region": "PL",
    "IsSandbox": false,
    "ProviderConfig": {
      "merchantId": "123456",
      "terminalId": "TERM001",
      "protocolVersion": "2.0"
    }
  },
  "MP.FiscalPrinter": {
    "Enabled": true,
    "ProviderId": "posnet_thermal",
    "ConnectionType": "serial",
    "ConnectionSettings": {
      "PortName": "COM3",
      "BaudRate": 9600,
      "Parity": "None",
      "DataBits": 8,
      "StopBits": 1
    },
    "TaxRates": {
      "A": 23.0,
      "B": 8.0,
      "C": 5.0,
      "D": 0.0
    },
    "PrinterConfig": {
      "cashierName": "Admin",
      "nip": "1234567890",
      "headerLines": ["Shop Name", "Address Line 1", "NIP: 1234567890"]
    }
  }
}
```

## Supported Terminal Models

### Ingenico (Global Leader)
- **Lane/3000**: Entry-level countertop, TCP/IP, Telium 2
- **Lane/5000**: Mid-range countertop, TCP/IP, Telium 2
- **Lane/7000**: High-end countertop, TCP/IP, Telium Tetra
- **Lane/8000**: Premium countertop, TCP/IP, Telium Tetra
- **Desk/5000**: Modular PIN pad, TCP/IP
- **Move/5000**: Wireless mobile, Bluetooth + WiFi
- **Link/2500**: Compact mobile, Bluetooth

**Protocol**: Telium Manager (proprietary), ISO 8583
**Connection**: TCP/IP (port 8800), USB, Bluetooth

### Verifone (USA/Global)
- **VX520**: Countertop, Ethernet/Dial
- **VX680**: Wireless GPRS/3G/WiFi
- **VX820**: Dual display, Ethernet
- **P400**: Stripe Terminal certified, TCP/IP

**Protocol**: VIPA (Verifone Internet Protocol Architecture)
**Connection**: TCP/IP, Serial, USB

### PAX (China/Global)
- **A920**: Android smart terminal, WiFi/4G
- **S920**: Premium Android, dual display
- **IM30**: Mobile Bluetooth terminal

**Protocol**: PAXSTORE protocol, EMV standards
**Connection**: WiFi, 4G, Bluetooth, USB

### Cloud Terminals (REST API)
- **SumUp**: REST API + OAuth2
- **Square Terminal**: Square Terminal API
- **Stripe Terminal**: Stripe SDK + Connection Token

## Fiscal Printer Requirements by Country

### Poland
- **Required**: Fiscal memory module
- **Standards**: POSNET, ELZAB, NOVITUS certified
- **Protocols**: Serial (RS-232), USB, Ethernet
- **Receipt Requirements**: NIP, cashier name, fiscal number

### Czech Republic
- **Required**: EET (Electronic Evidence of Sales) online registration
- **Providers**: WINCIE, VAROS, EURO-50TE Mini
- **Protocol**: EET REST API

### Slovakia
- **Required**: eKasa online fiscal system
- **Protocol**: eKasa REST API

### Hungary
- **Required**: NAV online invoice reporting
- **Protocol**: NAV REST API

## Transaction Flow

### Payment Terminal Flow
```
1. Checkout initiated
2. Load tenant terminal settings from ABP Settings
3. Initialize terminal provider with connection settings
4. Check terminal status (CheckTerminalStatusAsync)
5. Authorize payment (AuthorizePaymentAsync)
6. Capture payment (CapturePaymentAsync)
7. Print fiscal receipt if fiscal printer enabled
8. Mark item as sold
9. Return transaction result
```

### Fiscal Receipt Flow
```
1. Payment completed successfully
2. Load fiscal printer settings from ABP Settings
3. Initialize fiscal printer provider
4. Prepare receipt data (items, tax, total)
5. Send print command to fiscal printer
6. Wait for fiscal number response
7. Store fiscal number with transaction
8. Return receipt details
```

## Security Considerations

### Sensitive Data
- Terminal IPs and ports → ABP Settings (encrypted)
- API keys and secrets → Azure Key Vault (production)
- Merchant credentials → ABP Settings (encrypted)

### PCI-DSS Compliance
- No card data stored in application
- Terminal handles card data (PCI PTS certified devices)
- Transaction tokens only stored
- TLS 1.2+ for all communications

## Implementation Plan

### Phase 1: Core Architecture ✅
- [x] ITerminalPaymentProvider interface
- [x] Factory pattern
- [x] Mock provider
- [x] Basic checkout flow

### Phase 2: Communication Layer 🔄
- [ ] ITerminalCommunication interface
- [ ] TCP/IP implementation
- [ ] USB implementation
- [ ] Serial port implementation
- [ ] REST API implementation

### Phase 3: Real Terminal Providers 📋
- [ ] Ingenico Lane/5000 (TCP/IP)
- [ ] Verifone VX520 (TCP/IP)
- [ ] PAX A920 (REST API)
- [ ] SumUp (REST API)
- [ ] Stripe Terminal (SDK)

### Phase 4: Fiscal Printers 📋
- [ ] IFiscalPrinterProvider interface
- [ ] Posnet provider (Serial/USB)
- [ ] Elzab provider (Serial/USB)
- [ ] EET Czech provider (REST API)

### Phase 5: ABP Settings Migration 📋
- [ ] Define settings structure
- [ ] Migration from DB to Settings
- [ ] Settings management UI
- [ ] Encryption for sensitive values

## Testing Strategy

### Unit Tests
- Mock providers
- Communication layer mocks
- Protocol parsers

### Integration Tests
- Terminal simulator
- Fiscal printer simulator
- End-to-end checkout flow

### Hardware Tests
- Real terminal devices
- Real fiscal printers
- Multiple currency testing
