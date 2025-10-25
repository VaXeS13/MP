-- OU-47: Data Migration Script
-- Assigns existing data to default organizational units
-- This script updates all 27 entities to have OrganizationalUnitId set to the default unit for their tenant
--
-- IMPORTANT: Run AFTER creating default units via OU-46 (DataMigrationHelper)
-- The default unit for each tenant should have code "{TenantCode}-MAIN"

SET IDENTITY_INSERT ON;

-- Create temporary table to store tenant → default unit mapping
CREATE TABLE #TenantDefaultUnits (
    TenantId UNIQUEIDENTIFIER NOT NULL,
    DefaultUnitId UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY (TenantId)
);

-- Populate mapping from OrganizationalUnits where IsMainUnit = 1
INSERT INTO #TenantDefaultUnits (TenantId, DefaultUnitId)
SELECT
    ou.TenantId,
    ou.Id
FROM AppOrganizationalUnits ou
INNER JOIN AppOrganizationalUnitSettings ous ON ou.Id = ous.OrganizationalUnitId
WHERE ous.IsMainUnit = 1;

-- OU-47: Update all 27 entities with OrganizationalUnitId
-- Core Business Entities (14)

-- 1. Booths
UPDATE AppBooths
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppBooths.TenantId = tdtu.TenantId
  AND AppBooths.OrganizationalUnitId IS NULL;

-- 2. BoothTypes
UPDATE AppBoothTypes
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppBoothTypes.TenantId = tdtu.TenantId
  AND AppBoothTypes.OrganizationalUnitId IS NULL;

-- 3. Rentals
UPDATE AppRentals
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppRentals.TenantId = tdtu.TenantId
  AND AppRentals.OrganizationalUnitId IS NULL;

-- 4. RentalExtensionPayments
UPDATE AppRentalExtensionPayments
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppRentalExtensionPayments.TenantId = tdtu.TenantId
  AND AppRentalExtensionPayments.OrganizationalUnitId IS NULL;

-- 5. Carts
UPDATE AppCarts
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppCarts.TenantId = tdtu.TenantId
  AND AppCarts.OrganizationalUnitId IS NULL;

-- 6. CartItems
UPDATE AppCartItems
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppCartItems.TenantId = tdtu.TenantId
  AND AppCartItems.OrganizationalUnitId IS NULL;

-- 7. Items
UPDATE AppItems
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppItems.TenantId = tdtu.TenantId
  AND AppItems.OrganizationalUnitId IS NULL;

-- 8. ItemSheets
UPDATE AppItemSheets
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppItemSheets.TenantId = tdtu.TenantId
  AND AppItemSheets.OrganizationalUnitId IS NULL;

-- 9. FloorPlans
UPDATE AppFloorPlans
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppFloorPlans.TenantId = tdtu.TenantId
  AND AppFloorPlans.OrganizationalUnitId IS NULL;

-- 10. Settlements
UPDATE AppSettlements
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppSettlements.TenantId = tdtu.TenantId
  AND AppSettlements.OrganizationalUnitId IS NULL;

-- 11. Promotions
UPDATE AppPromotions
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppPromotions.TenantId = tdtu.TenantId
  AND AppPromotions.OrganizationalUnitId IS NULL;

-- 12. PromotionUsages
UPDATE AppPromotionUsages
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppPromotionUsages.TenantId = tdtu.TenantId
  AND AppPromotionUsages.OrganizationalUnitId IS NULL;

-- 13. HomePageSections
UPDATE AppHomePageSections
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppHomePageSections.TenantId = tdtu.TenantId
  AND AppHomePageSections.OrganizationalUnitId IS NULL;

-- 14. ChatMessages (nullable, so only update if null)
UPDATE AppChatMessages
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppChatMessages.TenantId = tdtu.TenantId
  AND AppChatMessages.OrganizationalUnitId IS NULL;

-- Payment Transactions (3)

-- 15. P24Transactions
UPDATE AppP24Transactions
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppP24Transactions.TenantId = tdtu.TenantId
  AND AppP24Transactions.OrganizationalUnitId IS NULL;

-- 16. StripeTransactions
UPDATE AppStripeTransactions
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppStripeTransactions.TenantId = tdtu.TenantId
  AND AppStripeTransactions.OrganizationalUnitId IS NULL;

-- 17. PayPalTransactions
UPDATE AppPayPalTransactions
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppPayPalTransactions.TenantId = tdtu.TenantId
  AND AppPayPalTransactions.OrganizationalUnitId IS NULL;

-- Device & Terminal Settings (2)

-- 18. TenantTerminalSettings
UPDATE AppTenantTerminalSettings
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppTenantTerminalSettings.TenantId = tdtu.TenantId
  AND AppTenantTerminalSettings.OrganizationalUnitId IS NULL;

-- 19. TenantFiscalPrinterSettings
UPDATE AppTenantFiscalPrinterSettings
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppTenantFiscalPrinterSettings.TenantId = tdtu.TenantId
  AND AppTenantFiscalPrinterSettings.OrganizationalUnitId IS NULL;

-- Local Agent & Security (1)

-- 20. AgentApiKeys
UPDATE AppAgentApiKeys
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppAgentApiKeys.TenantId = tdtu.TenantId
  AND AppAgentApiKeys.OrganizationalUnitId IS NULL;

-- File Management & Notifications (2) - Nullable fields

-- 21. UploadedFiles (nullable, so only update if null)
UPDATE AppUploadedFiles
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppUploadedFiles.TenantId = tdtu.TenantId
  AND AppUploadedFiles.OrganizationalUnitId IS NULL;

-- 22. UserNotifications (nullable, so only update if null)
UPDATE AppUserNotifications
SET OrganizationalUnitId = tdtu.DefaultUnitId
FROM #TenantDefaultUnits tdtu
WHERE AppUserNotifications.TenantId = tdtu.TenantId
  AND AppUserNotifications.OrganizationalUnitId IS NULL;

-- Organizational Unit Domain (5)

-- 23-27. AppOrganizationalUnits, AppUserOrganizationalUnits,
-- AppOrganizationalUnitSettings, AppOrganizationalUnitRegistrationCodes
-- These are organizational unit management tables - no need to update

-- Cleanup
DROP TABLE #TenantDefaultUnits;

-- Verification: Log summary of updates
DECLARE @TotalBooths INT = (SELECT COUNT(*) FROM AppBooths WHERE OrganizationalUnitId IS NOT NULL);
DECLARE @TotalRentals INT = (SELECT COUNT(*) FROM AppRentals WHERE OrganizationalUnitId IS NOT NULL);
DECLARE @TotalCarts INT = (SELECT COUNT(*) FROM AppCarts WHERE OrganizationalUnitId IS NOT NULL);
DECLARE @TotalItems INT = (SELECT COUNT(*) FROM AppItems WHERE OrganizationalUnitId IS NOT NULL);

PRINT 'OU-47 Migration Summary:';
PRINT '- Booths assigned: ' + CAST(@TotalBooths AS VARCHAR(10));
PRINT '- Rentals assigned: ' + CAST(@TotalRentals AS VARCHAR(10));
PRINT '- Carts assigned: ' + CAST(@TotalCarts AS VARCHAR(10));
PRINT '- Items assigned: ' + CAST(@TotalItems AS VARCHAR(10));
PRINT 'Data migration to default organizational units completed.';

SET IDENTITY_INSERT OFF;
