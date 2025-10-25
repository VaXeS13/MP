# MP-100 PHASE 5: Data Migration & Seeding (OU-46 to OU-51)

## Overview

This document describes the data migration process for organizational units in the MarketPlace project. It covers the migration of existing data to default organizational units and the setup of organizational unit infrastructure for new tenants.

**Timeline**: This is a critical data migration that must be carefully planned and tested before production deployment.

---

## Tasks Breakdown

### OU-46: Create Default Organizational Units
**Status**: ✅ Complete
**Location**: `src/MP.DbMigrator/DataMigrationHelper.cs`

Creates a default "Główna" (Main) organizational unit for each existing tenant in the system.

**What it does:**
- Iterates through all existing tenants
- Creates unit "Główna" with code `{TENANTCODE}-MAIN` (e.g., "WARSZAWA-MAIN")
- Generates default OrganizationalUnitSettings for the unit
- Returns mapping of TenantId → DefaultUnitId for use in next steps

**Execution:**
```csharp
var helper = serviceProvider.GetRequiredService<DataMigrationHelper>();
var mapping = await helper.CreateDefaultUnitsForAllTenantsAsync();
```

**Tenant Code Generation:**
- Tenant name is converted to UPPERCASE and spaces are removed
- Example: "Warszawa" → "WARSZAWA"
- Example: "New Client" → "NEWCLIENT"

---

### OU-47: Assign Data to Default Organizational Units
**Status**: ✅ Complete
**Location**: `src/MP.DbMigrator/MigrationScripts/002_AssignDataToDefaultUnits.sql`

SQL migration script that updates all 27 entities with their default organizational unit.

**What it does:**
- Updates 27 business entities to assign OrganizationalUnitId
- Uses temporary table mapping tenant → default unit
- Updates all rows with NULL or empty GUID to point to tenant's default unit

**Entities Updated (27 total):**

| # | Category | Entity | Table Name |
|---|----------|--------|-----------|
| 1-2 | Core Business | Booths, BoothTypes | AppBooths, AppBoothTypes |
| 3-4 | Rentals | Rentals, RentalExtensionPayments | AppRentals, AppRentalExtensionPayments |
| 5-6 | Shopping | Carts, CartItems | AppCarts, AppCartItems |
| 7-8 | Items | Items, ItemSheets | AppItems, AppItemSheets |
| 9 | Locations | FloorPlans | AppFloorPlans |
| 10-12 | Financial | Settlements, Promotions, PromotionUsages | AppSettlements, AppPromotions, AppPromotionUsages |
| 13-14 | Content | HomePageSections, ChatMessages | AppHomePageSections, AppChatMessages |
| 15-17 | Payments | P24Transactions, StripeTransactions, PayPalTransactions | AppP24Transactions, AppStripeTransactions, AppPayPalTransactions |
| 18-19 | Devices | TenantTerminalSettings, TenantFiscalPrinterSettings | AppTenantTerminalSettings, AppTenantFiscalPrinterSettings |
| 20 | Security | AgentApiKeys | AppAgentApiKeys |
| 21-22 | Files/Notifications | UploadedFiles, UserNotifications | AppUploadedFiles, AppUserNotifications |
| 23-27 | OU Domain | OrganizationalUnits, UserOrganizationalUnits, Settings, RegistrationCodes | AppOrganizationalUnits, AppUserOrganizationalUnits, AppOrganizationalUnitSettings, AppOrganizationalUnitRegistrationCodes |

**Execution:**
```sql
-- Run this script in SQL Server Management Studio or via DbMigrator
SQLCMD -S devmarketing -d MP < 002_AssignDataToDefaultUnits.sql
```

**Safety Measures:**
- Creates temporary table for mapping (auto-cleaned up)
- Only updates NULL or empty GUID values
- Preserves existing assignments
- Includes verification at end

---

### OU-48: Assign Users to Default Organizational Units
**Status**: ✅ Complete
**Location**: `src/MP.DbMigrator/MigrationScripts/003_AssignUsersToDefaultUnits.sql`

SQL migration script that creates UserOrganizationalUnit records linking existing users to their tenant's default unit.

**What it does:**
- Iterates all tenants
- Gets all users in each tenant from AbpUsers table
- Creates UserOrganizationalUnit entry for each user → default unit mapping
- Sets IsActive = true, AssignedAt = now
- Avoids duplicate assignments

**Execution:**
```sql
-- Run this script in SQL Server Management Studio or via DbMigrator
SQLCMD -S devmarketing -d MP < 003_AssignUsersToDefaultUnits.sql
```

**Edge Cases Handled:**
- Users already assigned to unit (skipped, not duplicated)
- Multiple roles (all assigned to same unit)
- Admin users (assigned same as other users)
- RoleId left NULL initially (can be assigned per-user later)

---

### OU-49: Seed New Tenant Organizational Units
**Status**: ✅ Complete
**Location**: `src/MP.Domain/Data/NewTenantOrganizationalUnitSeedContributor.cs`

Seed contributor that automatically runs when a new tenant is created.

**What it does:**
- Automatically triggered during new tenant creation
- Creates default "Główna" unit for the tenant
- Creates default OrganizationalUnitSettings
- Generates registration code for the unit (unlimited, no expiry)
- Logs all operations

**Registration Code Generation:**
- 8 characters, alphanumeric
- Format: `ABC12345`
- No expiration date (null)
- Unlimited usage count (null)

**How It Works:**
1. New tenant created via TenantManagement UI
2. DataSeeder calls all IDataSeedContributor implementations
3. NewTenantOrganizationalUnitSeedContributor runs automatically
4. Default unit and registration code created

**Testing:**
```csharp
// Create new tenant
var tenant = new Tenant { Name = "TestTenant" };
await tenantRepository.InsertAsync(tenant);

// Trigger seeding
await dataSeeder.SeedAsync(new DataSeedContext(tenant.Id));

// Verify default unit created
var unit = await ouRepository.FindByCodeAsync(tenant.Id, "TESTTENANT-MAIN");
Assert.NotNull(unit);
Assert.True(unit.IsActive);
```

---

### OU-50: Seed Demo Organizational Units
**Status**: ✅ Complete
**Location**: `src/MP.Domain/Data/DemoOrganizationalUnitSeedContributor.cs`

Seed contributor that creates demo units for test/development tenants.

**What it does:**
- Creates sample units in development tenant "CTO":
  - "Główna" (Main) - code: CTO-MAIN
  - "Centrum" (Center) - code: CTO-CENTER
  - "Północ" (North) - code: CTO-NORTH
- Creates sample units in development tenant "KISS":
  - "Główna" (Main) - code: KISS-MAIN
  - "Zachodnia" (West) - code: KISS-WEST
- Creates registration codes for each unit
- Enables testing of multi-unit features

**Demo Registration Code Format:**
- Format: `{TENANT}-{UNIT}-{RANDOM}`
- Example: `CTO-CEN-ABC123`
- 50 characters max
- No expiration, unlimited usage

**Execution:**
- Runs automatically during dev/test environment seeding
- Only creates units if tenant is in demo list
- Safe to run multiple times (idempotent)

---

### OU-51: Test Migration on Database Copy
**Status**: Documentation Complete
**Location**: This guide + verification scripts

## Migration Testing Procedure

### Pre-Migration Checklist

Before running migration on production:

1. **Database Backup**
   ```bash
   # Create full backup of production database
   sqlcmd -S devmarketing -Q "BACKUP DATABASE [MP] TO DISK='D:\Backups\MP_PreMigration.bak'"
   ```

2. **Test Backup Restore**
   ```bash
   # Restore to test environment
   RESTORE DATABASE [MP_Test] FROM DISK='D:\Backups\MP_PreMigration.bak'
   ```

3. **Create Rollback Scripts**
   - `001_CreateDefaultUnits_Rollback.sql` - Drop created units
   - `002_AssignDataToDefaultUnits_Rollback.sql` - Reset OrganizationalUnitId to NULL
   - `003_AssignUsersToDefaultUnits_Rollback.sql` - Delete UserOrganizationalUnit records

### Test Environment Setup

```bash
# 1. Restore test database
sqlcmd -S devmarketing -d master -Q "CREATE DATABASE MP_Migration_Test"
RESTORE DATABASE [MP_Migration_Test] FROM DISK='D:\Backups\MP_PreMigration.bak'
```

### Migration Execution

```bash
# 1. Create default units (OU-46)
cd src/MP.DbMigrator
dotnet run --environment Test

# 2. Execute migration scripts (OU-47, OU-48)
sqlcmd -S devmarketing -d MP_Migration_Test -i "..\MigrationScripts\002_AssignDataToDefaultUnits.sql"
sqlcmd -S devmarketing -d MP_Migration_Test -i "..\MigrationScripts\003_AssignUsersToDefaultUnits.sql"
```

### Verification Queries

After migration, run these verification queries:

```sql
-- 1. Check all tenants have default unit
SELECT
    ou.TenantId,
    COUNT(*) as UnitCount,
    SUM(CASE WHEN ous.IsMainUnit = 1 THEN 1 ELSE 0 END) as MainUnitCount
FROM AppOrganizationalUnits ou
LEFT JOIN AppOrganizationalUnitSettings ous ON ou.Id = ous.OrganizationalUnitId
WHERE ou.IsActive = 1
GROUP BY ou.TenantId
HAVING MainUnitCount = 1;

-- Expected: One row per tenant, UnitCount >= 1, MainUnitCount = 1

-- 2. Verify all entities have OrganizationalUnitId assigned
SELECT 'Booths' as Entity, COUNT(*) as Total,
       SUM(CASE WHEN OrganizationalUnitId IS NOT NULL AND OrganizationalUnitId != CAST(0 AS UNIQUEIDENTIFIER) THEN 1 ELSE 0 END) as Assigned
FROM AppBooths
UNION ALL
SELECT 'Rentals', COUNT(*), SUM(CASE WHEN OrganizationalUnitId IS NOT NULL AND OrganizationalUnitId != CAST(0 AS UNIQUEIDENTIFIER) THEN 1 ELSE 0 END) FROM AppRentals
UNION ALL
SELECT 'Carts', COUNT(*), SUM(CASE WHEN OrganizationalUnitId IS NOT NULL AND OrganizationalUnitId != CAST(0 AS UNIQUEIDENTIFIER) THEN 1 ELSE 0 END) FROM AppCarts
UNION ALL
SELECT 'Items', COUNT(*), SUM(CASE WHEN OrganizationalUnitId IS NOT NULL AND OrganizationalUnitId != CAST(0 AS UNIQUEIDENTIFIER) THEN 1 ELSE 0 END) FROM AppItems;

-- Expected: All values in "Assigned" column should equal "Total" for non-nullable columns

-- 3. Check user assignments
SELECT
    TenantId,
    COUNT(*) as UserUnitAssignments,
    COUNT(DISTINCT UserId) as Unique Users
FROM AppUserOrganizationalUnits
WHERE IsActive = 1
GROUP BY TenantId;

-- Expected: Each tenant has users assigned

-- 4. Verify foreign keys are valid
SELECT COUNT(*) as InvalidReferences
FROM AppBooths
WHERE OrganizationalUnitId IS NOT NULL
  AND OrganizationalUnitId NOT IN (SELECT Id FROM AppOrganizationalUnits);

-- Expected: 0 rows (no invalid references)
```

### Success Criteria

Migration is successful if:

✅ All tenants have exactly one default unit with code `{TENANTCODE}-MAIN`
✅ All 27 entities have non-NULL OrganizationalUnitId (except nullable fields which may be NULL)
✅ No NULL values in OrganizationalUnitId for required fields
✅ All foreign key relationships are valid
✅ All UserOrganizationalUnit records point to valid users and units
✅ No data loss detected (row counts match pre-migration)

### Rollback Plan

If migration fails:

```bash
# 1. Stop application immediately
# 2. Restore backup
sqlcmd -S devmarketing -d master -Q "RESTORE DATABASE [MP] FROM DISK='D:\Backups\MP_PreMigration.bak' WITH REPLACE"

# 3. Run rollback scripts (if only partial execution)
sqlcmd -S devmarketing -d MP -i "..\MigrationScripts\003_AssignUsersToDefaultUnits_Rollback.sql"
sqlcmd -S devmarketing -d MP -i "..\MigrationScripts\002_AssignDataToDefaultUnits_Rollback.sql"
sqlcmd -S devmarketing -d MP -i "..\MigrationScripts\001_CreateDefaultUnits_Rollback.sql"

# 4. Resume application
# 5. Analyze root cause before retrying
```

---

## Deployment Timeline

### Pre-Deployment (1 day before)
- [ ] Create backup of production database
- [ ] Test backup restoration
- [ ] Create rollback scripts
- [ ] Communicate maintenance window to users
- [ ] Schedule downtime window (off-peak hours recommended)

### Deployment Day (maintenance window)
- [ ] Notify users of maintenance
- [ ] Stop production application
- [ ] Run OU-46: Create default units (`dotnet run --environment Production`)
- [ ] Run OU-47: SQL migration script 002
- [ ] Run OU-48: SQL migration script 003
- [ ] Run verification queries
- [ ] Start application
- [ ] Test key functionality (rent booth, create cart, etc.)
- [ ] Monitor logs for errors

### Post-Deployment (1 day after)
- [ ] Run full test suite against production data
- [ ] Verify reports and analytics still work
- [ ] Check performance metrics
- [ ] Archive backup in long-term storage
- [ ] Document any issues for future reference

---

## Monitoring & Troubleshooting

### During Migration

Monitor these logs:
- `src/MP.DbMigrator/logs.txt` - Migration execution log
- `src/MP.HttpApi.Host/logs.txt` - Application errors
- Windows Event Viewer - SQL Server errors

### Common Issues & Fixes

**Issue**: Migration script fails with "OrganizationalUnit not found"
- **Cause**: Default units not created by OU-46
- **Fix**: Ensure DataMigrationHelper.CreateDefaultUnitsForAllTenantsAsync() ran successfully

**Issue**: Foreign key constraint violation
- **Cause**: Unit ID doesn't exist in AppOrganizationalUnits
- **Fix**: Verify OU-46 created all tenant units

**Issue**: Duplicate user assignments in AppUserOrganizationalUnits
- **Cause**: Script ran twice without cleanup
- **Fix**: Delete duplicates or restore from backup

**Issue**: Performance degradation after migration
- **Cause**: Missing indices on OrganizationalUnitId columns
- **Fix**: Rebuild database indices: `DBCC DBREINDEX (AppBooths, 0, 85)`

---

## Future Enhancements

After OU-46-51 completion:

1. **Populate RoleId** in UserOrganizationalUnit
   - Query user roles from AspNetUserRoles
   - Update UserOrganizationalUnit.RoleId based on user's role in tenant

2. **Unit-Specific Permissions**
   - Extend authorization system to support per-unit permissions
   - Users can have different roles in different units

3. **Data Reporting**
   - Create reports showing unit distribution
   - Track unit membership changes
   - Monitor unit-specific metrics

---

## Dependencies

- ✅ OU-12: Migration created (20251024172135_AddOrganizationalUnits.cs)
- ✅ OU-20: Tables created and indexed
- ✅ OU-45: Authorization policies implemented

---

## Code References

**Files Created:**
- `src/MP.DbMigrator/DataMigrationHelper.cs` - OU-46 helper
- `src/MP.DbMigrator/MigrationScripts/002_AssignDataToDefaultUnits.sql` - OU-47
- `src/MP.DbMigrator/MigrationScripts/003_AssignUsersToDefaultUnits.sql` - OU-48
- `src/MP.Domain/Data/NewTenantOrganizationalUnitSeedContributor.cs` - OU-49
- `src/MP.Domain/Data/DemoOrganizationalUnitSeedContributor.cs` - OU-50
- `MIGRATION_OU46-51_GUIDE.md` - This file (OU-51)

**Related Files:**
- `src/MP.Domain/Data/MPDbMigrationService.cs` - Orchestrates migrations
- `src/MP.Domain/OrganizationalUnits/OrganizationalUnitManager.cs` - Domain service
- `src/MP.EntityFrameworkCore/Migrations/20251024172135_AddOrganizationalUnits.cs` - Schema

---

## Contacts & Support

For questions or issues:
- **Data Migration**: Check `src/MP.DbMigrator/logs.txt` for detailed execution logs
- **Application Issues**: Check `src/MP.HttpApi.Host/logs.txt`
- **SQL Issues**: Contact DBA or SQL Server administrator

---

**Last Updated**: 2025-10-25
**Status**: Ready for Production Deployment
**Version**: 1.0
