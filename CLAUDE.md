Zawsze używaj pliku @RULES.md
# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an ABP Framework-based layered monolith application with an Angular frontend. The project follows Domain Driven Design (DDD) practices and supports multi-tenancy.
It is functionally similar to [Kirpparikalle](https://www.kirpparikalle.net/) but with enhanced features,  
including **interactive floor-plan mapping**, multi-floor support, and flexible payment providers.

## Key Commands

### .NET Backend Commands
- **Build solution**: `dotnet build MP.sln`
- **Run API Host**: `dotnet run --project src/MP.HttpApi.Host/MP.HttpApi.Host.csproj`
- **Run API Host with hot reload**: `dotnet watch --project src/MP.HttpApi.Host/MP.HttpApi.Host.csproj`
- **Run database migrations**: `dotnet run --project src/MP.DbMigrator/MP.DbMigrator.csproj`
- **Clean solution**: `dotnet clean MP.sln`
- **Run all tests**: `dotnet test MP.sln`
- **Run specific test project**: `dotnet test test/MP.Application.Tests/MP.Application.Tests.csproj`
- **Run single test**: `dotnet test --filter "FullyQualifiedName~TestMethodName"`
- **Run tests with coverage**: `dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover`

### Angular Frontend Commands (in `/angular` directory)
- **Install dependencies**: `npm install`
- **Development server**: `ng serve` or `npm start` (runs on http://localhost:4200)
- **Development server on different port**: `ng serve --port 4201`
- **Build for production**: `ng build --configuration production` or `npm run build:prod`
- **Build with watch mode**: `ng build --watch --configuration development`
- **Run tests**: `ng test` or `npm test`
- **Run tests with coverage**: `ng test --code-coverage`
- **Lint code**: `ng lint` or `npm run lint`
- **Generate component**: `ng generate component component-name`
- **Generate service**: `ng generate service service-name`

### ABP-Specific Commands
- **Install client-side packages**: `abp install-libs` (run in solution root)
- **Generate ABP service proxy**: `abp generate-proxy -t ng` (run in angular directory after creating new backend services)
- **Update ABP packages**: `abp update` (run in solution root)

### Entity Framework Migrations
- **Add new migration**: `dotnet ef migrations add MigrationName --project src/MP.EntityFrameworkCore/MP.EntityFrameworkCore.csproj`
- **Remove last migration**: `dotnet ef migrations remove --project src/MP.EntityFrameworkCore/MP.EntityFrameworkCore.csproj`
- **Apply migrations**: Run `MP.DbMigrator` project or use `dotnet ef database update --project src/MP.EntityFrameworkCore/MP.EntityFrameworkCore.csproj`

## Architecture

## Multitenancy and Hostnames
- **Tenant resolution** via subdomain.
- Development example:  
  - Tenant `warszawa` → `warszawa.localhost:4200`
- Production example:  
  - Tenant `warszawa` → `warszawa.mp.com`
- ABP multi-tenant middleware resolves tenant from subdomain and falls back to default host if necessary.


### Backend (.NET 9.0)
The solution follows ABP Framework's layered architecture:

- **MP.Domain**: Core business logic and domain entities
- **MP.Domain.Shared**: Shared domain types (DTOs, enums, constants)
- **MP.Application**: Application services and business logic implementation
- **MP.Application.Contracts**: Service interfaces and DTOs
- **MP.HttpApi**: Web API controllers
- **MP.HttpApi.Client**: HTTP client proxies
- **MP.HttpApi.Host**: Main web application host (runs on https://localhost:44377)
- **MP.EntityFrameworkCore**: Data access layer with Entity Framework Core
- **MP.DbMigrator**: Database migration console application

### Frontend (Angular 19)
- Built with Angular 19 and ABP Angular UI packages
- Uses PrimeNG components and Lepton-X theme
- Supports multi-tenancy with tenant-specific subdomains
- Charts support via Chart.js and ng2-charts
- Fabric.js integration for canvas operations

### Testing Projects
- **MP.TestBase**: Base test infrastructure
- **MP.Domain.Tests**: Domain layer tests
- **MP.Application.Tests**: Application layer tests
- **MP.EntityFrameworkCore.Tests**: Data access tests
- **MP.HttpApi.Client.ConsoleTestApp**: Console app for testing API client

### Key Business Domains

The application is built around these core domain concepts:

- **Rentals**: Booth rental management with periods, status tracking (Active, Extended, Completed, Cancelled), and payment integration
- **Booths**: Physical booth management with location tracking, types, currency support, and floor plan integration
- **Items**: Item management system with barcode generation (format: `{ItemSheetId}-{SequenceNumber}`), status tracking, and item sheets
- **Payments**: Multi-provider payment integration (Przelewy24, Stripe, PayPal) with transaction tracking
- **FloorPlans**: Interactive floor plan mapping with Fabric.js, supporting multiple floors and booth positioning via FloorPlanBooth and FloorPlanElement entities
- **Settlements**: Financial settlement management for booth rentals
- **Carts**: Shopping cart functionality with CartItem management
- **Notifications**: User notification system for real-time updates
- **Chat**: Real-time chat messaging via SignalR
- **Fiscal Printers & Terminals**: Integration with fiscal printing devices and payment terminals

#### Barcode System
- Items use UUID v4 for unique identification
- Barcodes are generated using `BarcodeHelper` with format: `{ItemSheetId}-{SequenceNumber}`
- Supports JsBarcode library for barcode rendering on frontend

## Development Setup

### Prerequisites
- .NET 9.0+ SDK
- Node.js v18 or v20
- SQL Server (connection configured for `devmarketing` server)

### First-time Setup
1. Run `abp install-libs` in solution root to install client-side dependencies
2. Run `MP.DbMigrator` to create and seed the initial database
3. Configure connection strings in `src/MP.HttpApi.Host/appsettings.json`

### Multi-Tenancy
- Multi-tenancy is enabled with shared database strategy
- Tenant domains configured: `localhost`, `*.localhost`, `cto.localhost:4200`
- Angular client URL pattern: `http://{tenant}.localhost:4200`

## Key Configuration

### API Host Configuration
- **Default URL**: https://localhost:44377
- **Database**: SQL Server (`devmarketing` server)
- **CORS Origins**: Configured for localhost:4200 and tenant subdomains
- **OpenIddict**: Uses certificate with passphrase `cd8a1828-5d08-495a-8c62-b5b37b27378c`

### Payment Integration
- **Przelewy24**: Configured for sandbox environment
- Merchant ID: 142798

### SSL Certificate Generation
For production environments, generate signing certificate:
```bash
dotnet dev-certs https -v -ep openiddict.pfx -p cd8a1828-5d08-495a-8c62-b5b37b27378c
```

### Important Configuration Files
- **Connection strings**: `src/MP.HttpApi.Host/appsettings.json` and `src/MP.DbMigrator/appsettings.json`
- **CORS settings**: `src/MP.HttpApi.Host/appsettings.json` (CorsOrigins section)
- **OpenIddict configuration**: `src/MP.Domain/OpenIddict/OpenIddictDataSeedContributor.cs`
- **Multi-tenancy settings**: `src/MP.Domain/MultiTenancy/SubdomainTenantResolveContributor.cs`
- **Angular environment**: `angular/src/environments/environment.ts` and `environment.prod.ts`
- **ABP settings**: Defined in `src/MP.Domain/Settings/MPSettingDefinitionProvider.cs`

## Development Workflow

1. Start the API: `dotnet run --project src/MP.HttpApi.Host/MP.HttpApi.Host.csproj`
2. Navigate to `angular/` directory and run: `ng serve`
3. Access the application at http://localhost:4200
4. API documentation available at https://localhost:44377/swagger

## Testing

### Backend Tests
All test projects are located in the `test/` directory:
- `test/MP.TestBase/` - Base test infrastructure and helpers
- `test/MP.Domain.Tests/` - Domain layer unit tests
- `test/MP.Application.Tests/` - Application service tests
- `test/MP.EntityFrameworkCore.Tests/` - Data access layer tests
- `test/MP.HttpApi.Client.ConsoleTestApp/` - Console app for API testing

#### Running Backend Tests
- **Run all tests**: `dotnet test MP.sln`
- **Run tests with verbose output**: `dotnet test MP.sln --verbosity detailed`
- **Run specific test project**: `dotnet test test/MP.Application.Tests/MP.Application.Tests.csproj`
- **Run specific test class**: `dotnet test --filter "FullyQualifiedName~ClassName"`
- **Run single test method**: `dotnet test --filter "FullyQualifiedName~ClassName.TestMethodName"`
- **Run tests with coverage**: `dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover`
- **Run tests with detailed coverage**: `dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=./coverage/`

### Frontend Tests
Located in `angular/` directory, using Jasmine and Karma:

```bash
cd angular
ng test                    # Run tests with watch mode
ng test --code-coverage   # Run tests with coverage report
ng test --no-watch        # Run tests once without watch
ng test --browsers=ChromeHeadless  # Run in headless mode for CI/CD
```

## Important Notes

### Code Standards (from RULES.md)
- **All code and comments must be written in English** - only user-facing messages can be in Polish
- **File length limit**: Keep files under 1000 lines of code - split into smaller modules if needed
- **No TODO comments**: Complete all tasks fully - never leave TODO comments in code
- **Install dependencies first**: Always install dependencies before generating code that uses them
- **Ask for help**: If stuck after 2 attempts, ask the user for assistance

### ABP Framework
- Always run `abp install-libs` after adding new ABP packages
- Use `MP.DbMigrator` after adding new database migrations
- When adding new Angular services that interact with backend APIs, regenerate service proxies using `abp generate-proxy -t ng` in the angular directory
- The project uses .NET 9.0 and Angular 19

### Frontend
- SCSS is used for styling in Angular components
- Frontend uses UUID v4 for unique identifiers - import from 'uuid' package
- Canvas operations are handled via Fabric.js integration for floor-plan mapping
- PrimeNG components with Lepton-X theme for UI
- Chart.js and ng2-charts for data visualization
- PDF generation via jsPDF library (v3.0.3)
- Barcode rendering with JsBarcode library

### Backend
- Health checks available at `/health-status`
- API documentation at https://localhost:44377/swagger

### SignalR Real-time Communication
- Backend provides SignalR hubs for real-time features (chat, notifications)
- Frontend uses @microsoft/signalr package (v9.0.6) for connection
- SignalR endpoints available at API host URL
- Used for live updates in chat messaging and user notifications