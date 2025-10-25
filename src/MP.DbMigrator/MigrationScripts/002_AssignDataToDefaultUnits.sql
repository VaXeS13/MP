-- OU-47: Data Migration Script
-- Assigns existing data to default organizational units
-- This script updates all 27 entities to have OrganizationalUnitId set to the default unit for their tenant
--
-- IMPORTANT: Run AFTER creating default units via OU-46 (DataMigrationHelper)
-- The default unit for each tenant should have code "{TenantCode}-MAIN"

-- OU-47: Update all 27 entities with OrganizationalUnitId
-- Core Business Entities (14)

-- Get the default unit ID (assuming one main unit exists, or use first by creation)
DECLARE @DefaultUnitId UNIQUEIDENTIFIER;
SELECT TOP 1 @DefaultUnitId = ou.Id
FROM AppOrganizationalUnits ou
INNER JOIN AppOrganizationalUnitSettings ous ON ou.Id = ous.OrganizationalUnitId
WHERE ous.IsMainUnit = 1
ORDER BY ou.CreationTime;

IF @DefaultUnitId IS NULL
BEGIN
    RAISERROR('No default organizational unit found. Run OU-46 first.', 16, 1);
    RETURN;
END;

PRINT 'Using default unit: ' + CAST(@DefaultUnitId AS VARCHAR(36));

-- 1. Booths
UPDATE AppBooths
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'Booths: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 2. BoothTypes
UPDATE AppBoothTypes
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'BoothTypes: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 3. Rentals
UPDATE AppRentals
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'Rentals: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 4. RentalExtensionPayments
UPDATE AppRentalExtensionPayments
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'RentalExtensionPayments: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 5. Carts
UPDATE AppCarts
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'Carts: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 6. CartItems
UPDATE AppCartItems
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'CartItems: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 7. Items
UPDATE AppItems
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'Items: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 8. ItemSheets
UPDATE AppItemSheets
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'ItemSheets: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 9. FloorPlans
UPDATE AppFloorPlans
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'FloorPlans: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 10. Settlements
UPDATE AppSettlements
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'Settlements: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 11. Promotions
UPDATE AppPromotions
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'Promotions: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 12. PromotionUsages
UPDATE AppPromotionUsages
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'PromotionUsages: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 13. HomePageSections
UPDATE AppHomePageSections
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'HomePageSections: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 14. ChatMessages (nullable)
UPDATE AppChatMessages
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'ChatMessages: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- Payment Transactions (3)

-- 15. P24Transactions
UPDATE AppP24Transactions
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'P24Transactions: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 16. StripeTransactions
UPDATE AppStripeTransactions
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'StripeTransactions: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 17. PayPalTransactions
UPDATE AppPayPalTransactions
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'PayPalTransactions: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- Device & Terminal Settings (2)

-- 18. TenantTerminalSettings
UPDATE AppTenantTerminalSettings
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'TenantTerminalSettings: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 19. TenantFiscalPrinterSettings
UPDATE AppTenantFiscalPrinterSettings
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'TenantFiscalPrinterSettings: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- Local Agent & Security (1)

-- 20. AgentApiKeys
UPDATE AppAgentApiKeys
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'AgentApiKeys: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- File Management & Notifications (2) - Nullable fields

-- 21. UploadedFiles (nullable)
UPDATE AppUploadedFiles
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'UploadedFiles: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- 22. UserNotifications (nullable)
UPDATE AppUserNotifications
SET OrganizationalUnitId = @DefaultUnitId
WHERE OrganizationalUnitId IS NULL;
PRINT 'UserNotifications: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' updated';

-- Organizational Unit Domain (5)
-- 23-27. AppOrganizationalUnits, AppUserOrganizationalUnits,
-- AppOrganizationalUnitSettings, AppOrganizationalUnitRegistrationCodes
-- These are organizational unit management tables - no need to update

-- Final verification
DECLARE @TotalBooths INT = (SELECT COUNT(*) FROM AppBooths WHERE OrganizationalUnitId IS NOT NULL);
DECLARE @TotalRentals INT = (SELECT COUNT(*) FROM AppRentals WHERE OrganizationalUnitId IS NOT NULL);
DECLARE @TotalCarts INT = (SELECT COUNT(*) FROM AppCarts WHERE OrganizationalUnitId IS NOT NULL);
DECLARE @TotalItems INT = (SELECT COUNT(*) FROM AppItems WHERE OrganizationalUnitId IS NOT NULL);

PRINT '';
PRINT '=== OU-47 Migration Summary ===';
PRINT 'Total Booths assigned: ' + CAST(@TotalBooths AS VARCHAR(10));
PRINT 'Total Rentals assigned: ' + CAST(@TotalRentals AS VARCHAR(10));
PRINT 'Total Carts assigned: ' + CAST(@TotalCarts AS VARCHAR(10));
PRINT 'Total Items assigned: ' + CAST(@TotalItems AS VARCHAR(10));
PRINT 'Data migration to default organizational units completed.';
