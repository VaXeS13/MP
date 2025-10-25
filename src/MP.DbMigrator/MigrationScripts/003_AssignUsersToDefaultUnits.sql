-- OU-48: User Assignment Migration Script
-- Assigns existing users to their tenant's default organizational unit
-- This script creates UserOrganizationalUnit records mapping users to their default unit
--
-- IMPORTANT: Run AFTER creating default units via OU-46 and assigning data via OU-47

SET IDENTITY_INSERT ON;

-- Create temporary table to store tenant → default unit mapping
CREATE TABLE #TenantDefaultUnits (
    TenantId UNIQUEIDENTIFIER NOT NULL,
    DefaultUnitId UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY (TenantId)
);

-- Populate mapping from OrganizationalUnitSettings where IsMainUnit = 1
INSERT INTO #TenantDefaultUnits (TenantId, DefaultUnitId)
SELECT
    ou.TenantId,
    ou.Id
FROM AppOrganizationalUnits ou
INNER JOIN AppOrganizationalUnitSettings ous ON ou.Id = ous.OrganizationalUnitId
WHERE ous.IsMainUnit = 1
  AND ou.TenantId IS NOT NULL;

-- OU-48: Create UserOrganizationalUnit assignments for existing users
-- For each user in a tenant, assign them to that tenant's default organizational unit

DECLARE @TenantId UNIQUEIDENTIFIER;
DECLARE @DefaultUnitId UNIQUEIDENTIFIER;
DECLARE @UserId UNIQUEIDENTIFIER;
DECLARE @RoleId UNIQUEIDENTIFIER;

DECLARE tenant_cursor CURSOR FOR
SELECT DISTINCT TenantId, DefaultUnitId FROM #TenantDefaultUnits;

OPEN tenant_cursor;
FETCH NEXT FROM tenant_cursor INTO @TenantId, @DefaultUnitId;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- For each tenant, get all users and assign them to default unit
    -- Users are identified from AbpUsers table with matching TenantId

    DECLARE user_cursor CURSOR FOR
    SELECT DISTINCT UserId, NULL AS RoleId  -- RoleId can be extended later
    FROM (
        -- Get users assigned to this tenant via user-role relationships
        SELECT u.Id AS UserId
        FROM AbpUsers u
        WHERE u.TenantId = @TenantId
          AND u.Id NOT IN (
            -- Exclude users already assigned to this unit
            SELECT UserId
            FROM AppUserOrganizationalUnits
            WHERE TenantId = @TenantId
              AND OrganizationalUnitId = @DefaultUnitId
          )
    ) AS UniqueUsers;

    OPEN user_cursor;
    FETCH NEXT FROM user_cursor INTO @UserId, @RoleId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Insert UserOrganizationalUnit record for this user
        IF NOT EXISTS (
            SELECT 1
            FROM AppUserOrganizationalUnits
            WHERE UserId = @UserId
              AND OrganizationalUnitId = @DefaultUnitId
              AND TenantId = @TenantId
        )
        BEGIN
            INSERT INTO AppUserOrganizationalUnits (
                Id,
                UserId,
                OrganizationalUnitId,
                RoleId,
                TenantId,
                IsActive,
                AssignedAt,
                CreationTime,
                CreatorId
            )
            VALUES (
                NEWID(),                           -- Id (GUID)
                @UserId,                           -- UserId
                @DefaultUnitId,                    -- OrganizationalUnitId (default unit)
                @RoleId,                           -- RoleId (null, can be assigned later)
                @TenantId,                         -- TenantId
                1,                                 -- IsActive = true
                GETUTCDATE(),                      -- AssignedAt = now
                GETUTCDATE(),                      -- CreationTime = now
                NULL                               -- CreatorId = null
            );
        END;

        FETCH NEXT FROM user_cursor INTO @UserId, @RoleId;
    END;

    CLOSE user_cursor;
    DEALLOCATE user_cursor;

    FETCH NEXT FROM tenant_cursor INTO @TenantId, @DefaultUnitId;
END;

CLOSE tenant_cursor;
DEALLOCATE tenant_cursor;

-- Cleanup
DROP TABLE #TenantDefaultUnits;

-- Verification: Log summary of assignments
DECLARE @TotalAssignments INT = (SELECT COUNT(*) FROM AppUserOrganizationalUnits WHERE IsActive = 1);
DECLARE @TotalUsers INT = (SELECT COUNT(DISTINCT UserId) FROM AppUserOrganizationalUnits WHERE IsActive = 1);

PRINT 'OU-48 Migration Summary:';
PRINT '- Total user assignments created: ' + CAST(@TotalAssignments AS VARCHAR(10));
PRINT '- Total unique users assigned: ' + CAST(@TotalUsers AS VARCHAR(10));
PRINT 'User assignment to default organizational units completed.';

SET IDENTITY_INSERT OFF;
