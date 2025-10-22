# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

**IMPORTANT**: Always refer to RULES.md for code standards and collaboration practices.

## Project Overview

This is an ABP Framework-based (v9.2.0) layered monolith application built with .NET 9.0 and Angular 19. The project follows Domain Driven Design (DDD) practices and supports multi-tenancy with subdomain-based tenant resolution.

**Business Context**: Functionally similar to [Kirpparikalle](https://www.kirpparikalle.net/) - a flea market/booth rental management system with enhanced features including:
- Interactive floor-plan mapping with Fabric.js
- Multi-floor support
- Flexible payment providers (Przelewy24, Stripe, PayPal)
- Real-time features via SignalR (chat, notifications)
- Barcode system for item management
- Multi-currency support per tenant

## Key Commands

### .NET Backend Commands
- **Build solution**: `dotnet build MP.sln`
- **Clean solution**: `dotnet clean MP.sln`
- **Run API Host**: `dotnet run --project src/MP.HttpApi.Host/MP.HttpApi.Host.csproj`
- **Run API Host with hot reload**: `dotnet watch --project src/MP.HttpApi.Host/MP.HttpApi.Host.csproj`
- **Run database migrations**: `dotnet run --project src/MP.DbMigrator/MP.DbMigrator.csproj`

### Testing Commands
- **Run all tests**: `dotnet test MP.sln`
- **Run tests with verbose output**: `dotnet test MP.sln --verbosity detailed`
- **Run specific test project**: `dotnet test test/MP.Application.Tests/MP.Application.Tests.csproj`
- **Run specific test class**: `dotnet test --filter "FullyQualifiedName~ClassName"`
- **Run single test method**: `dotnet test --filter "FullyQualifiedName~ClassName.TestMethodName"`
- **Run tests with coverage**: `dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover`
- **Run tests with detailed coverage**: `dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=./coverage/`

### Running Tests by Category
**Domain Tests (fastest - unit tests)**
```
dotnet test test/MP.Domain.Tests/MP.Domain.Tests.csproj --no-build -v q
```

**Booth Tests**
```
dotnet test test/MP.Application.Tests/MP.Application.Tests.csproj --no-build --filter "BoothAppServiceTests"
```

**Rental Tests**
```
dotnet test test/MP.Application.Tests/MP.Application.Tests.csproj --no-build --filter "RentalAppServiceTests"
```

**Cart Tests**
```
dotnet test test/MP.Application.Tests/MP.Application.Tests.csproj --no-build --filter "CartAppServiceTests"
```

**Payment Tests**
```
dotnet test test/MP.Application.Tests/MP.Application.Tests.csproj --no-build --filter "DailyBoothStatusSyncJobTests|P24StatusCheckRecurringJobTests"
```

### Current Test Status (Phase 2 Complete)
- **Total**: 38/50 tests passing (76%)
- **Domain**: 12/12 (100%) ✅
- **Booth**: 6/6 (100%) ✅
- **Rental**: 13/14 (93%)
- **Cart**: 8/12 (67%)
- **Payment Jobs**: 5/10 (50%)

**Remaining Issues** (12 failing tests):
- Cart: 4 failing tests (business logic: UpdateItem, Checkout recalculation)
- Rental: 1 failing test (GetMyRentalsAsync - data pollution with DbContext disposal)
- Payment: 7 failing tests (P24StatusCheckRecurringJob - complex payment logic, DbContext scoping)

### Angular Frontend Commands (in `/angular` directory)
- **Install dependencies**: `npm install`
- **Development server**: `ng serve` or `npm start` (runs on http://localhost:4200)
- **Development server on different port**: `ng serve --port 4201`
- **Build for production**: `ng build --configuration production` or `npm run build:prod`
- **Build with watch mode**: `ng build --watch --configuration development`
- **Run tests**: `ng test` or `npm test`
- **Run tests with coverage**: `ng test --code-coverage`
- **Run tests once without watch**: `ng test --no-watch`
- **Run tests in headless mode (CI/CD)**: `ng test --browsers=ChromeHeadless`
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
- **Apply migrations to database**: `dotnet ef database update --project src/MP.EntityFrameworkCore/MP.EntityFrameworkCore.csproj`
- **Preferred migration approach**: Run `MP.DbMigrator` project for migrations (includes seeding)

### Database and Migration Notes

**Important Guidelines:**
- **Always use `MP.DbMigrator`** for production and development - it handles both migrations and data seeding consistently
- **Never use `dotnet ef database update`** directly in production - use the migrator console app instead
- **Multi-tenancy consideration**: All data includes `TenantId` for isolation - migrations must account for this
- **Connection string location**: Update in both `src/MP.HttpApi.Host/appsettings.json` and `src/MP.DbMigrator/appsettings.json`

**After Adding/Modifying Entities:**
1. Create migration: `dotnet ef migrations add DescriptiveName --project src/MP.EntityFrameworkCore`
2. Review generated migration files in `src/MP.EntityFrameworkCore/Migrations/` - ensure data seeding safety
3. Run `MP.DbMigrator` project to apply migration and seed data
4. Test with multiple tenants to verify data isolation works correctly
5. If adding navigation properties, update repository methods with `.WithDetails()` includes

**Common Migration Issues:**
- **Shadow properties not in migration**: Add navigation properties to DbContext entity configuration
- **Seeding failures**: Ensure seed contributors handle tenant-specific data correctly
- **Data loss with concurrent operations**: Use transactions via `[UnitOfWork]` attribute
- **Migration naming**: Use PascalCase and be descriptive (e.g., `AddRentalExtensionFields`, not `Update1`)

## Architecture

### Backend (.NET 9.0)
The solution follows ABP Framework's layered architecture with these projects:

**Core Layers:**
- **MP.Domain**: Core business logic, domain entities, repositories interfaces, domain services
  - Key managers: `RentalManager`, `BoothManager`, `CartManager`, `ItemManager`, `PromotionManager`, `HomePageSectionManager`
  - Data seeders: `OpenIddictDataSeedContributor`, `BoothDataSeedContributor`
  - Extended Identity: `AppUser` with bank account information
- **MP.Domain.Shared**: Shared domain types (DTOs, enums, constants, localization)
- **MP.Application**: Application services, business logic implementation, background workers
  - Workers: `ExpiredCartCleanupWorker` (runs every 5 minutes to release expired cart reservations)
- **MP.Application.Contracts**: Service interfaces and DTOs (data transfer objects)

**Infrastructure Layers:**
- **MP.EntityFrameworkCore**: Data access layer with Entity Framework Core
  - DbContext configuration and migrations
  - Repository implementations
  - Separate repositories per aggregate (e.g., `RentalRepository`, `BoothRepository`, `CartRepository`)
- **MP.HttpApi**: Web API controllers exposing application services
  - Auto-generated controllers from application services
  - RESTful API endpoints
- **MP.HttpApi.Client**: HTTP client proxies for consuming APIs
- **MP.HttpApi.Host**: Main web application host (runs on https://localhost:44377)
  - Includes Swagger UI, CORS configuration, authentication/authorization
  - Razor Pages for authentication (Login, Register, Profile)
  - SignalR hub endpoints for real-time features

**Utility Projects:**
- **MP.DbMigrator**: Database migration and seeding console application
- **MP.TestBase**: Base test infrastructure and helpers

### Frontend (Angular 19.1.0)
- Built with Angular 19.1.0 and ABP Angular UI packages (v9.2.0)
- Uses PrimeNG components (v19.1.4) with Lepton-X theme (v4.2.0)
- TypeScript 5.6.0 for type-safe development
- Supports multi-tenancy with dynamic subdomain-based tenant resolution
- Charts support via Chart.js (v4.5.0) and ng2-charts (v8.0.0)
- Fabric.js (v6.7.1) integration for interactive canvas operations (floor plans)
- Real-time communication with @microsoft/signalr (v9.0.6)
- PDF generation via jsPDF (v3.0.3)
- Barcode rendering with JsBarcode (v3.12.1)
- UUID generation via uuid (v13.0.0)
- Testing with Jasmine and Karma

**Key Angular Services:**
- `TenantCurrencyService`: Manages tenant-specific currency settings with caching
- `CartService`: Shopping cart management
- `RentalService`: Rental operations
- `UserProfileService`: User profile and account management
- `HomePageContentService`: Homepage content management
- SignalR Hubs for real-time chat and notifications

### Testing Projects
- **MP.TestBase**: Base test infrastructure with helpers and mocks
  - Provides test base classes for each layer
  - In-memory database setup for testing
  - Mock configurations for external services
- **MP.Domain.Tests**: Domain layer unit tests
  - Tests for domain services and managers
  - Value object validation tests
  - Business rule enforcement tests
- **MP.Application.Tests**: Application service integration tests
  - Full stack integration tests with database
  - Tests application service methods
  - Authorization and permission tests
- **MP.EntityFrameworkCore.Tests**: Data access layer tests
  - Repository implementation tests
  - Database query tests
  - Migration tests
- **MP.HttpApi.Client.ConsoleTestApp**: Console app for manual API testing

### Key Business Domains

**Rentals**
- Booth rental management with date periods and status tracking
- Status workflow: Draft → Active → (Extended) → Completed/Cancelled
- Payment integration with multiple providers
- Rental extension system with additional payments

**Booths**
- Physical booth management with location tracking
- Types, sizes, and pricing configuration
- Floor plan integration via FloorPlanBooth entities
- Multi-floor support with positioning

**Items**
- Item management system with barcode generation
- Barcode format: `{ItemSheetId}-{SequenceNumber}`
- Item sheets for batch management
- Status tracking: Available, Sold, Reserved, etc.
- UUID v4 for unique identification

**Payments**
- Multi-provider integration:
  - **Przelewy24**: Polish payment gateway (sandbox: 142798)
  - **Stripe**: International cards and payment methods
  - **PayPal**: PayPal and credit cards
- Transaction tracking and status management
- Payment method enum: Cash, Card, Online, BankTransfer

**FloorPlans**
- Interactive floor plan mapping with Fabric.js canvas
- Multiple floors per location
- Booth positioning via FloorPlanBooth entities
- FloorPlanElement for visual elements (walls, doors, etc.)

**Carts**
- Shopping cart with CartItem management
- Booth reservation system with expiration (ReservationExpiresAt)
- Cart status: Active, Completed, Abandoned
- Background worker cleans up expired reservations every 5 minutes

**Settlements**
- Financial settlement management for booth rentals
- Commission calculations
- Settlement periods and payouts

**Notifications & Chat**
- User notification system for real-time updates
- Real-time chat messaging via SignalR hubs
- Push notifications for important events

**Fiscal Printers & Terminals**
- Integration with fiscal printing devices
- Payment terminal integration
- Receipt generation

**User Management**
- Extended user profiles with bank account numbers
- Profile management pages (Razor Pages and Angular)
- User-specific dashboard for rentals and items
- Account settings and preferences

**Homepage Content**
- Dynamic homepage section management
- Customizable content per tenant
- Section ordering and visibility controls
- Rich content support for landing pages

**Promotions**
- Promotional campaign management
- Discount and special offer tracking
- Time-based promotion scheduling

### Barcode System
- Items use UUID v4 for unique identification
- Barcodes generated using `BarcodeHelper` with format: `{ItemSheetId}-{SequenceNumber}`
- Frontend uses JsBarcode library for barcode rendering
- Supports various barcode formats (CODE128, EAN13, etc.)

### Domain-Driven Design Patterns Used
- **Aggregates**: `Rental`, `Booth`, `Cart`, `Item`, `FloorPlan` serve as aggregate roots
- **Value Objects**: `RentalPeriod`, `Money` (for currency handling)
- **Domain Events**: Published for rental status changes, payment completions
- **Domain Services**: Manager classes (`RentalManager`, `CartManager`) for complex business logic
- **Repository Pattern**: Interfaces in Domain layer, implementations in EntityFrameworkCore
- **Specification Pattern**: Used for complex queries (e.g., booth availability checks)

## Multi-Tenancy Architecture

### Tenant Resolution
- **Strategy**: Subdomain-based tenant resolution
- **Database**: Shared database strategy (all tenants in one database)
- **Implementation**: `SubdomainTenantResolveContributor` in Domain layer

### Hostname Patterns
**Development:**
- Host tenant (no subdomain): `localhost:4200`
- Tenant `warszawa`: `warszawa.localhost:4200`
- Tenant `cto`: `cto.localhost:4200`

**Production:**
- Host tenant: `mp.com`
- Tenant `warszawa`: `warszawa.mp.com`

### OAuth Configuration
- Dynamic OAuth client IDs per tenant: `MP_App_{TenantName}`
- Host tenant uses: `MP_App`
- Redirect URIs configured per subdomain
- Tenant names stored in UPPERCASE in database

### Tenant-Specific Features
- Currency per tenant (PLN, EUR, USD, GBP, CZK)
- Cached via `TenantCurrencyService` on frontend
- Booth configurations and floor plans per tenant
- Isolated rental and item management
- Separate data partitioning using `TenantId` in database
- Each tenant has own OAuth client configuration
- Customizable homepage content per tenant

## Development Setup

### Prerequisites
- .NET 9.0+ SDK
- Node.js v18 or v20
- SQL Server (configured for `devmarketing` server)
- Redis (optional, for caching - localhost:6379)

### First-time Setup
1. Clone repository
2. Run `abp install-libs` in solution root to install client-side dependencies
3. Configure connection string in `src/MP.HttpApi.Host/appsettings.json`
4. Run `MP.DbMigrator` to create and seed the initial database
5. Navigate to `angular/` directory and run `npm install`
6. Start API: `dotnet run --project src/MP.HttpApi.Host/MP.HttpApi.Host.csproj`
7. Start Angular: `cd angular && ng serve`
8. Access application at http://localhost:4200

### Development Workflow
1. **Backend development**: Use hot reload with `dotnet watch`
2. **Frontend development**: Use `ng serve` with live reload
3. **Database changes**:
   - Create EF migration: `dotnet ef migrations add MigrationName --project src/MP.EntityFrameworkCore/MP.EntityFrameworkCore.csproj`
   - Apply migration: Run `MP.DbMigrator` project
4. **API changes**: After adding/modifying backend services, run `abp generate-proxy -t ng` in angular directory
5. **Testing**: Run backend and frontend tests regularly
6. **Multi-tenant testing**:
   - Test host tenant: `http://localhost:4200`
   - Test specific tenant: `http://{tenantname}.localhost:4200` (e.g., `http://cto.localhost:4200`)
7. **Debugging**:
   - Backend: Use Visual Studio or VS Code with C# extension
   - Frontend: Use Chrome DevTools, Angular DevTools extension
8. **Payment Testing** (Stripe):
   - **Webhook Forwarding** (REQUIRED for local testing):
     - Option A: Stripe CLI: `stripe listen --forward-to http://localhost:44377/api/app/payments/stripe/webhook`
     - Option B: ngrok: `ngrok http 44377` (configure webhook in Stripe Dashboard)
   - **Configure webhook secret**: Copy from Stripe CLI output or Dashboard to `appsettings.json`
   - **Enable Stripe provider**: Set `MP.PaymentProviders.Stripe.Enabled = true` in Settings
   - **Test flow**:
     1. Add booth to cart
     2. Checkout with Stripe payment
     3. Use test card: `4242424242424242`
     4. Verify webhook received (check Stripe CLI output)
     5. Verify rental marked as paid, booth status changed to Rented
   - **Detailed instructions**: See `STRIPE_SETUP.md`

## Configuration

### API Host Configuration
- **Default URL**: https://localhost:44377
- **Database**: SQL Server (`devmarketing` server)
- **Connection String**: `Server=devmarketing;Database=MP;Trusted_Connection=True;TrustServerCertificate=true`
- **CORS Origins**: `http://localhost:4200,http://*.localhost:4200,https://localhost:44377`
- **Redirect URLs**: Configured for localhost and tenant subdomains

### OpenIddict (OAuth/OpenID Connect)
- Certificate passphrase: `cd8a1828-5d08-495a-8c62-b5b37b27378c`
- Swagger client ID: `MP_Swagger`
- Dynamic client IDs per tenant: `MP_App_{TenantName}`
- For production, generate certificate: `dotnet dev-certs https -v -ep openiddict.pfx -p cd8a1828-5d08-495a-8c62-b5b37b27378c`

### Payment Providers Configuration

**Przelewy24 (Polish Payment Gateway):**
- Sandbox URL: `https://sandbox.przelewy24.pl`
- Merchant ID: 142798
- Used for Polish market payments

**Stripe (International Payment Processing):**
- **Status**: ✅ Fully implemented and tested
- **Account ID**: `acct_1SEbgBQihiXumQfX` (Test Mode)
- **Implementation**: Stripe Checkout Sessions with webhook integration
- **Test mode API**: `https://api.stripe.com`
- **Test publishable key**: `pk_test_51SEbgBQihiXumQfXroAsiten17Fq45ismKEFprs9xtHfcNvtece3fsj5e7IsKSSysvFhMHg2YT5LHP6UeQs5nud6003qa4dfdb`
- **Test secret key**: `sk_test_51SEbgBQihiXumQfXiJxoinXPhzMtGPtfOC7zHtKwhCsHjnIACnauSHczuaFQ3yjRX583ynGFN6XW4IhWfkwxmbBQ00MlgoLWQo`
- **⚠️ Webhook secret**: MUST be configured (see STRIPE_SETUP.md)
- **Webhook endpoint**: `POST /api/app/payments/stripe/webhook`
- **Supported payment methods**:
  - Credit/Debit Cards (Visa, Mastercard, Amex)
  - Google Pay
  - Apple Pay
  - Klarna (EUR, SEK, NOK, DKK only)
- **Test cards**:
  - `4242424242424242` - Success
  - `4000002500003155` - Requires 3D Secure authentication
  - `4000000000009995` - Declined
- **Products created**:
  - Product: "Booth Rental" (`prod_TH7YyaTAAWdDl2`)
  - Prices: 100 PLN, 200 PLN, 300 PLN
- **Documentation**:
  - **Setup Guide**: See `STRIPE_SETUP.md` for configuration instructions
  - **Implementation Analysis**: See `STRIPE_IMPLEMENTATION_ANALYSIS.md` for detailed code review
- **Key Files**:
  - Provider: `src/MP.Application/Payments/StripeProvider.cs`
  - Webhook Handler: `src/MP.Application/Payments/StripeWebhookHandler.cs`
  - Controller: `src/MP.HttpApi/Controllers/PaymentController.cs`
  - Transaction Entity: `src/MP.Domain/Payments/StripeTransaction.cs`

**PayPal:**
- Sandbox URL: `https://www.sandbox.paypal.com`

### Redis Cache (Optional)
- Default connection: `localhost:6379`
- Default sliding expiration: 15 minutes
- Used for caching tenant settings, application cache

### Important Configuration Files
- **Connection strings**: `src/MP.HttpApi.Host/appsettings.json`, `src/MP.DbMigrator/appsettings.json`
- **CORS settings**: `src/MP.HttpApi.Host/appsettings.json` → `App:CorsOrigins`
- **Payment providers**: `src/MP.HttpApi.Host/appsettings.json` → `PaymentProviders`
- **OpenIddict configuration**: `src/MP.Domain/OpenIddict/OpenIddictDataSeedContributor.cs`
- **Multi-tenancy settings**: `src/MP.Domain/MultiTenancy/SubdomainTenantResolveContributor.cs`
- **Angular environment**: `angular/src/environments/environment.ts` and `environment.prod.ts`
- **ABP settings**: `src/MP.Domain/Settings/MPSettingDefinitionProvider.cs`
- **Permissions**: `src/MP.Application.Contracts/Permissions/MPPermissions.cs` and `MPPermissionDefinitionProvider.cs`
- **Localization**: `src/MP.Domain.Shared/Localization/MP/en.json` and `pl-PL.json`
- **AutoMapper profiles**: `src/MP.Application/MPApplicationAutoMapperProfile.cs`

## Important Development Notes

### Code Standards (from RULES.md)
- **Communication Language**: Always respond to the user in Polish ("Zawsze odpowiadaj w języku polskim")
- **Code Language**: All code and comments MUST be written in English (only user-facing messages in Polish)
- **File length limit**: Keep files under 1000 lines - split into smaller modules if needed
- **No TODO comments**: Complete all tasks fully - never leave TODO comments in code
- **Dependency installation**: Always install dependencies BEFORE generating code that uses them
- **Transparency**: If stuck after 2 attempts, ask the user for help
- **Pair programming approach**: Collaborate with user to generate best solutions (human-in-the-loop)

### Common Test Patterns

**Test Structure (Application Layer Integration Tests):**
```csharp
[Fact]
public async Task MethodName_Should_ExpectedBehavior_When_Condition()
{
    // Arrange
    var input = new CreateUpdateDto { /* ... */ };

    // Act
    var result = await _appService.MethodName(input);

    // Assert
    result.Should().NotBeNull();
    result.Property.Should().Be(expected);
}
```

**Using TestData and Fixtures:**
- Define test data constants in test classes (e.g., `TestUserId1`, `TestBootId`)
- Use `GuidGenerator.Create()` for generating entity IDs in tests
- Use `WithUnitOfWorkAsync()` to wrap multi-step test scenarios with database transactions
- Avoid `[UnitOfWork]` attribute on test methods - use `WithUnitOfWorkAsync()` instead for better isolation

**Testing Multi-Tenant Scenarios:**
- Use `using (CurrentTenant.Change(tenantId))` to switch tenant context
- Always verify `TenantId` is set correctly on created entities
- Test without tenant context (host tenant) separately from tenant-specific tests

**Database Assertion Tips:**
- Always reload entities from database before assertions to catch real persistence issues
- Use `await repository.GetAsync(id, includeDetails: true)` to ensure navigation properties are loaded
- Check dates carefully - use future dates for rentals, avoid date overlap issues
- Verify business invariants: booth availability, rental periods, cart expiration

### Performance Considerations

**Caching Strategy:**
- **Frontend**: Use `shareReplay(1)` on `TenantCurrencyService` - avoids redundant API calls
- **Backend**: Cache booth pricing, tenant settings, and currency rates - invalidate on changes
- **Redis**: Configure for tenant currency caching (15-minute sliding expiration)
- **Query optimization**: Always use `.WithDetails()` for navigation properties to avoid N+1 queries

**Query Optimization Tips:**
- **Specification pattern**: Use for complex queries with multiple includes and filters
- **Pagination**: Implement on list endpoints to handle large datasets (booths, items, rentals)
- **Indexing**: Keep database indexes current - review slow queries in logs
- **Lazy loading disabled**: Explicitly include related data to catch missing includes at dev time

**Database Performance:**
- **Connection pooling**: Ensure connection strings have `Max Pool Size` appropriate for workload
- **Batch operations**: Use batch create/update for bulk data changes (seeding, imports)
- **Soft deletes**: Use `IsDeleted` flag instead of hard deletes for audit trail - filter in queries

**Background Worker Performance:**
- **ExpiredCartCleanupWorker** runs every 5 minutes - monitor CPU impact on high-traffic periods
- **P24StatusCheckRecurringJob** - batch check payments to avoid individual API calls per payment
- Log execution time and record counts for monitoring - use structured logging with durations

### ABP Framework Best Practices
- Always run `abp install-libs` after adding new ABP packages
- Run `MP.DbMigrator` after creating new database migrations
- Regenerate service proxies after backend API changes: `abp generate-proxy -t ng` (in angular directory)
- Never skip DbMigrator - it handles seeding and ensures database consistency

### Frontend Development
- **Styling**: SCSS for component styles, PrimeFlex for utility classes
- **Identifiers**: Use UUID v4 from 'uuid' package for unique IDs
- **Canvas operations**: Fabric.js for floor-plan interactions
- **Components**: PrimeNG components with Lepton-X theme
- **State management**: RxJS BehaviorSubjects for shared state
- **Caching**: Use shareReplay(1) for cacheable observables

### Backend Development
- **Domain services**: Use manager classes for business logic (e.g., RentalManager, CartManager)
- **Repositories**: Define interfaces in Domain, implement in EntityFrameworkCore
- **Background workers**: Use IHostedService/BackgroundService for periodic tasks
- **Unit of Work**: Use [UnitOfWork] attribute for transactional operations
- **Logging**: Use ILogger<T> for structured logging
- **Validation**: Use DataAnnotations and FluentValidation for input validation
- **Authorization**: Use ABP permission system with permission names defined in `MPPermissions`
- **Exception Handling**: Use `BusinessException` for domain exceptions with localized messages
- **Auto Mapping**: Use AutoMapper profiles (e.g., `MPApplicationAutoMapperProfile`)

### Endpoints
- **API Base URL**: https://localhost:44377
- **Swagger UI**: https://localhost:44377/swagger
- **Health Checks**: https://localhost:44377/health-status
- **SignalR Hubs**: Connect via API base URL
- **Authentication Pages**:
  - Login: https://localhost:44377/Account/Login
  - Register: https://localhost:44377/Account/Register
  - Profile: https://localhost:44377/Account/Profile
- **OpenIddict Endpoints**:
  - Authorization: https://localhost:44377/connect/authorize
  - Token: https://localhost:44377/connect/token
  - UserInfo: https://localhost:44377/connect/userinfo

### Background Workers
- **ExpiredCartCleanupWorker**: Runs every 5 minutes
  - Releases expired booth reservations from carts
  - Soft-deletes associated Draft rentals
  - Keeps CartItem in cart for user visibility
  - Frees booths for other users to reserve

### Common Pitfalls
- Don't build the solution automatically - let user handle builds
- Always check tenant context when working with multi-tenant data
- Ensure proper authorization policies are applied to endpoints
- Validate date ranges for rentals (no overlapping periods per booth)
- Handle currency conversions when tenant currency differs from booth currency
- Cache expensive operations (currency lookups, tenant settings)
- Remember that tenant names are stored in UPPERCASE in database but subdomains are lowercase
- When creating OAuth clients, use format `MP_App_{TenantName}` where TenantName is UPPERCASE
- Don't forget to run `abp generate-proxy -t ng` after backend API changes
- Always use `GuidGenerator.Create()` for generating new entity IDs (not `Guid.NewGuid()`)
- When using repositories with `async` methods, always await the results
- Don't access navigation properties without including them in queries (`WithDetails` method)
- Background workers need proper scope creation for dependency injection

### Localization (i18n)
- **Supported Languages**: English (en) and Polish (pl-PL)
- **Location**: `src/MP.Domain.Shared/Localization/MP/`
- **Files**: `en.json` (English), `pl-PL.json` (Polish)
- **Usage in Backend**: Inject `IStringLocalizer<MPResource>` or use `L["ResourceKey"]` in services
- **Usage in Frontend**: Use ABP's `LocalizationService` or `localization` pipe
- **User-facing messages**: Always in Polish as per RULES.md
- **Code and technical messages**: Always in English

### Permissions and Authorization
- **Permission Provider**: `MPPermissionDefinitionProvider` in `Application.Contracts`
- **Permission Constants**: `MPPermissions` class with nested static classes per module
- **Permission Structure**:
  - `MP.Rentals` - Rental management permissions
  - `MP.Booths` - Booth management permissions
  - `MP.Items` - Item management permissions
  - `MP.FloorPlans` - Floor plan management permissions
  - `MP.Payments` - Payment management permissions
  - `MP.Dashboard` - Dashboard access permissions
- **Usage**: Apply `[Authorize(MPPermissions.Rentals.Create)]` attribute on app services
- **Policy-based Authorization**: Configure in `MPHttpApiHostModule`

### Project Structure Best Practices
- **Domain Layer**:
  - Place entities in folders by aggregate (e.g., `Rentals/`, `Booths/`)
  - Keep value objects near their aggregate roots
  - Domain services go in same folder as entities they manage
- **Application Layer**:
  - Application services mirror domain aggregates structure
  - DTOs in `Application.Contracts` project
  - Use folders: `/Rentals`, `/Booths`, `/Items`, etc.
- **Frontend**:
  - Feature modules in `angular/src/app/`
  - Shared components in `angular/src/app/shared/`
  - Proxy services generated in `angular/src/app/proxy/`
  - Environment configs in `angular/src/environments/`

### Debugging and Troubleshooting

**Common Issues and Solutions:**

1. **Multi-tenancy not working**:
   - Check `SubdomainTenantResolveContributor` is properly registered
   - Verify tenant name is UPPERCASE in database
   - Check browser URL format: `http://{tenantname}.localhost:4200`
   - Inspect `HttpContext.Items["ResolvedTenantName"]` in logs

2. **OAuth/OpenIddict errors**:
   - Ensure client ID matches tenant: `MP_App_{TENANTNAME}` in UPPERCASE
   - Check redirect URIs are properly configured for subdomain
   - Verify `openiddict.pfx` certificate exists with correct passphrase
   - Run `MP.DbMigrator` to seed OAuth clients for new tenants

3. **Entity Framework errors**:
   - Always include navigation properties: `await repository.GetAsync(id, includeDetails: true)`
   - Use `WithDetails()` for custom includes
   - Don't forget `[UnitOfWork]` attribute for transactional methods
   - Run migrations with `MP.DbMigrator`, not `dotnet ef database update`

4. **Angular proxy generation issues**:
   - Run `abp generate-proxy -t ng` in `angular/` directory, not solution root
   - Ensure API is running before generating proxies
   - Check `angular/src/app/proxy/generate-proxy.json` configuration
   - Delete `angular/src/app/proxy/` folder and regenerate if corrupted

5. **Background worker not executing**:
   - Ensure worker is registered in `MPApplicationModule`
   - Use `IServiceScopeFactory` to create scopes for dependency injection
   - Check worker logs in console output
   - Verify `[UnitOfWork]` attribute on worker methods accessing database

**Logging and Monitoring:**
- Backend logs: Console output and `Logs/logs.txt` in `MP.HttpApi.Host`
- Adjust log levels in `appsettings.json` → `Logging:LogLevel`
- Use structured logging: `_logger.LogInformation("Processing {EntityId}", entityId)`
- Frontend console: Check browser console for errors and network requests

### Example Scenarios

**Creating a New Feature (e.g., "Reviews" module):**

1. **Domain Layer** (`MP.Domain`):
   - Create `Reviews/Review.cs` entity
   - Create `Reviews/IReviewRepository.cs` interface
   - Create `Reviews/ReviewManager.cs` domain service
   - Add navigation properties to related entities

2. **Domain.Shared** (`MP.Domain.Shared`):
   - Create `Reviews/ReviewConsts.cs` for constants
   - Create `Reviews/ReviewStatus.cs` enum if needed
   - Add localization keys to `en.json` and `pl-PL.json`

3. **EntityFrameworkCore** (`MP.EntityFrameworkCore`):
   - Create `Reviews/EfCoreReviewRepository.cs`
   - Configure entity in `MPDbContext.OnModelCreating`
   - Add `DbSet<Review> Reviews` to `MPDbContext`
   - Create migration: `dotnet ef migrations add AddReviews --project src/MP.EntityFrameworkCore`
   - Run `MP.DbMigrator`

4. **Application.Contracts** (`MP.Application.Contracts`):
   - Create `Reviews/ReviewDto.cs`
   - Create `Reviews/CreateUpdateReviewDto.cs`
   - Create `Reviews/IReviewAppService.cs` interface
   - Add permissions to `MPPermissions.cs`: `public static class Reviews { ... }`
   - Define permissions in `MPPermissionDefinitionProvider.cs`

5. **Application** (`MP.Application`):
   - Create `Reviews/ReviewAppService.cs` implementing `IReviewAppService`
   - Add AutoMapper mappings to `MPApplicationAutoMapperProfile.cs`
   - Apply `[Authorize(MPPermissions.Reviews.Create)]` attributes

6. **Angular Frontend**:
   - Run `abp generate-proxy -t ng` in `angular/` directory
   - Create feature module: `ng generate module reviews --routing`
   - Create components: `ng generate component reviews/review-list`
   - Use generated proxy services in components
   - Add routes to `app-routing.module.ts`
   - Add menu items to `route.provider.ts`

**Adding a New Tenant:**

1. Use ABP's tenant management UI or create directly in database
2. Ensure tenant name is in UPPERCASE (e.g., "NEWCLIENT")
3. Run `MP.DbMigrator` to seed OAuth client for new tenant
4. Access tenant: `http://newclient.localhost:4200` (lowercase subdomain)
5. Login will use OAuth client `MP_App_NEWCLIENT` (uppercase)
6. Set tenant currency via settings management UI

## CI/CD and Deployment

### Pre-Deployment Checklist

**Code Quality:**
- All tests pass: `dotnet test MP.sln` (minimum: Domain + Booth at 100%)
- No compiler warnings: `dotnet build MP.sln` with no warnings
- Code review completed for all changes
- No hardcoded secrets or API keys in code

**Database:**
- All migrations applied via `MP.DbMigrator`
- Data seeding completed and validated
- Backup taken before production migration
- Migration tested in staging environment first

**Configuration:**
- `appsettings.json` updated with production values (connection strings, API keys)
- `openiddict.pfx` certificate generated with strong passphrase
- CORS origins updated for production domain
- Payment provider keys configured (Stripe webhook secret, etc.)
- Redis connection string configured if caching enabled

**Frontend:**
- Build production bundle: `npm run build:prod` in `angular/` directory
- No console errors in production build
- Environment files configured (`environment.prod.ts`)
- API endpoints point to production server

### Deployment Process

**Step 1: Build and Test**
```bash
dotnet clean MP.sln
dotnet build MP.sln
dotnet test MP.sln --verbosity minimal
```

**Step 2: Database Migration**
```bash
# In staging first
dotnet run --project src/MP.DbMigrator/MP.DbMigrator.csproj

# Verify data integrity
# Then deploy to production with backup
```

**Step 3: Backend Deployment**
- Publish API Host: `dotnet publish src/MP.HttpApi.Host/MP.HttpApi.Host.csproj -c Release`
- Deploy to hosting (IIS, Docker, Azure, etc.)
- Verify API is responding: `https://your-domain.com/swagger`
- Check health endpoint: `https://your-domain.com/health-status`

**Step 4: Frontend Deployment**
```bash
cd angular
npm ci  # Use package-lock.json for reproducible builds
npm run build:prod
# Deploy dist folder to web server or CDN
```

**Step 5: Post-Deployment Validation**
- Test login flow with OAuth
- Verify multi-tenant resolution (test subdomains)
- Test payment flow (use Stripe test mode initially)
- Check background worker execution (cart cleanup logs)
- Monitor error logs for unexpected exceptions
- Verify SignalR connections work for real-time features

### Monitoring and Logging

**Production Logging:**
- Set log levels in `appsettings.json` for production (avoid verbose logging)
- Monitor `Logs/logs.txt` for errors and warnings
- Use structured logging with context for troubleshooting
- Archive logs regularly to avoid disk space issues

**Key Metrics to Monitor:**
- API response times (target: <500ms for most endpoints)
- Database connection pool usage
- Payment processing success rate (stripe webhooks received)
- Background worker execution frequency
- Cart cleanup effectiveness (reservations released on schedule)
- User authentication success/failure rates

**Backup Strategy:**
- Daily automated database backups (retain 30 days)
- Regular code repository backups
- Document recovery procedures
- Test restoration process monthly

### Rollback Strategy

**If Deployment Fails:**
1. Revert code to last known good version
2. Keep database in current state (if migrations are backward compatible)
3. Restore from pre-deployment backup if data corruption occurred
4. Test thoroughly before re-deploying

**Database Rollback:**
- For schema changes: Use migrations to script rollback steps
- For data changes: Use transaction logs or restore from backup
- Always test rollback procedures before production deployment

## Useful Resources
- [ABP Framework Documentation](https://abp.io/docs/latest)
- [ABP Angular UI](https://abp.io/docs/latest/framework/ui/angular/quick-start)
- [Domain Driven Design in ABP](https://abp.io/docs/latest/framework/architecture/domain-driven-design)
- [ABP Multi-Tenancy](https://abp.io/docs/latest/framework/architecture/multi-tenancy)
- [PrimeNG Components](https://primeng.org/)
- [Fabric.js Documentation](http://fabricjs.com/docs/)
- [OpenIddict Documentation](https://documentation.openiddict.com/)
