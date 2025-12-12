-- =============================================
-- Migration: Fix ManagerStaff Permissions
-- Description: 
--   1. Remove PARTNER_UPDATE permission (no API exists)
--   2. Rename CONTRACT_SEND to CONTRACT_SEND_PDF (for clarity)
--   3. Remove REPORT and BOOKING permissions (these APIs don't require permissions)
-- Date: 2025-01-XX
-- =============================================

-- =============================================
-- Step 1: Remove PARTNER_UPDATE permission
-- =============================================
PRINT 'Removing PARTNER_UPDATE permission (no API exists)...';

-- First, remove any existing permissions granted to ManagerStaff
DELETE FROM [dbo].[ManagerStaffPartnerPermissions]
WHERE [permission_id] IN (
    SELECT [permission_id] 
    FROM [dbo].[Permissions] 
    WHERE [permission_code] = 'PARTNER_UPDATE'
);

-- Then remove the permission itself
DELETE FROM [dbo].[Permissions]
WHERE [permission_code] = 'PARTNER_UPDATE';

PRINT 'PARTNER_UPDATE permission removed successfully';
GO

-- =============================================
-- Step 2: Rename CONTRACT_SEND to CONTRACT_SEND_PDF
-- =============================================
PRINT 'Renaming CONTRACT_SEND to CONTRACT_SEND_PDF...';

-- Check if CONTRACT_SEND exists
IF EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'CONTRACT_SEND')
BEGIN
    -- Check if CONTRACT_SEND_PDF already exists
    IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'CONTRACT_SEND_PDF')
    BEGIN
        -- Update permission code
        UPDATE [dbo].[Permissions]
        SET 
            [permission_code] = 'CONTRACT_SEND_PDF',
            [permission_name] = N'Gửi hợp đồng PDF',
            [action_type] = 'SEND_PDF',
            [description] = N'Gửi hợp đồng PDF cho đối tác để ký'
        WHERE [permission_code] = 'CONTRACT_SEND';
        
        PRINT 'CONTRACT_SEND renamed to CONTRACT_SEND_PDF successfully';
    END
    ELSE
    BEGIN
        -- If CONTRACT_SEND_PDF already exists, migrate permissions and delete old one
        -- Update ManagerStaffPartnerPermissions to use new permission
        UPDATE msp
        SET msp.[permission_id] = p_new.[permission_id]
        FROM [dbo].[ManagerStaffPartnerPermissions] msp
        INNER JOIN [dbo].[Permissions] p_old ON msp.[permission_id] = p_old.[permission_id]
        INNER JOIN [dbo].[Permissions] p_new ON p_new.[permission_code] = 'CONTRACT_SEND_PDF'
        WHERE p_old.[permission_code] = 'CONTRACT_SEND';
        
        -- Delete old permission
        DELETE FROM [dbo].[Permissions]
        WHERE [permission_code] = 'CONTRACT_SEND';
        
        PRINT 'CONTRACT_SEND permissions migrated to CONTRACT_SEND_PDF and old permission removed';
    END
END
ELSE IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'CONTRACT_SEND_PDF')
BEGIN
    -- If CONTRACT_SEND doesn't exist and CONTRACT_SEND_PDF doesn't exist, create it
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
    ('CONTRACT_SEND_PDF', N'Gửi hợp đồng PDF', 'CONTRACT', 'SEND_PDF', N'Gửi hợp đồng PDF cho đối tác để ký');
    
    PRINT 'CONTRACT_SEND_PDF permission created';
END
ELSE
BEGIN
    PRINT 'CONTRACT_SEND_PDF permission already exists';
END
GO

-- =============================================
-- Step 3: Remove REPORT and BOOKING permissions
-- =============================================
PRINT 'Removing REPORT and BOOKING permissions (APIs accessible by default)...';

-- Remove REPORT permissions
DELETE FROM [dbo].[ManagerStaffPartnerPermissions]
WHERE [permission_id] IN (
    SELECT [permission_id] 
    FROM [dbo].[Permissions] 
    WHERE [resource_type] = 'REPORT'
);

DELETE FROM [dbo].[Permissions]
WHERE [resource_type] = 'REPORT';

PRINT 'REPORT permissions removed';

-- Remove BOOKING permissions
DELETE FROM [dbo].[ManagerStaffPartnerPermissions]
WHERE [permission_id] IN (
    SELECT [permission_id] 
    FROM [dbo].[Permissions] 
    WHERE [resource_type] = 'BOOKING'
);

DELETE FROM [dbo].[Permissions]
WHERE [resource_type] = 'BOOKING';

PRINT 'BOOKING permissions removed';
GO

-- =============================================
-- Step 3: Verify permissions
-- =============================================
PRINT 'Verifying permissions...';

-- List all ManagerStaff-related permissions (only PARTNER and CONTRACT)
SELECT 
    [permission_id],
    [permission_code],
    [permission_name],
    [resource_type],
    [action_type],
    [description],
    [is_active]
FROM [dbo].[Permissions]
WHERE [resource_type] IN ('PARTNER', 'CONTRACT')
ORDER BY [resource_type], [action_type];

PRINT '=============================================';
PRINT 'Permission Fix Migration completed successfully!';
PRINT '=============================================';
GO

