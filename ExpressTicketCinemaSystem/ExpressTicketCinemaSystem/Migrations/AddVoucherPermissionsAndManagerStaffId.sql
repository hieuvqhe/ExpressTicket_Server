-- =============================================
-- Migration: Add Voucher Permissions and manager_staff_id to Voucher table
-- Description: 
--   1. Thêm các quyền Voucher cho ManagerStaff (không cần partnerId)
--   2. Thêm trường manager_staff_id vào Voucher table để track ManagerStaff tạo/sửa voucher
-- Date: 2024-12-XX
-- =============================================

USE [ExpressTicketDB];
GO

PRINT '=============================================';
PRINT 'Starting migration: Add Voucher Permissions and manager_staff_id';
PRINT '=============================================';
GO

-- =============================================
-- Step 1: Add VOUCHER Permissions
-- =============================================

-- VOUCHER Permissions (không cần partnerId - global permission)
IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'VOUCHER_CREATE')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description], [is_active]) VALUES
    ('VOUCHER_CREATE', N'Tạo voucher', 'VOUCHER', 'CREATE', N'Tạo voucher mới', 1);
    PRINT 'Permission VOUCHER_CREATE created successfully';
END
ELSE
BEGIN
    PRINT 'Permission VOUCHER_CREATE already exists';
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'VOUCHER_READ')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description], [is_active]) VALUES
    ('VOUCHER_READ', N'Xem voucher', 'VOUCHER', 'READ', N'Xem danh sách và chi tiết voucher', 1);
    PRINT 'Permission VOUCHER_READ created successfully';
END
ELSE
BEGIN
    PRINT 'Permission VOUCHER_READ already exists';
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'VOUCHER_UPDATE')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description], [is_active]) VALUES
    ('VOUCHER_UPDATE', N'Cập nhật voucher', 'VOUCHER', 'UPDATE', N'Cập nhật thông tin voucher', 1);
    PRINT 'Permission VOUCHER_UPDATE created successfully';
END
ELSE
BEGIN
    PRINT 'Permission VOUCHER_UPDATE already exists';
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'VOUCHER_DELETE')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description], [is_active]) VALUES
    ('VOUCHER_DELETE', N'Xóa voucher', 'VOUCHER', 'DELETE', N'Xóa voucher (soft delete)', 1);
    PRINT 'Permission VOUCHER_DELETE created successfully';
END
ELSE
BEGIN
    PRINT 'Permission VOUCHER_DELETE already exists';
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'VOUCHER_SEND')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description], [is_active]) VALUES
    ('VOUCHER_SEND', N'Gửi voucher', 'VOUCHER', 'SEND', N'Gửi voucher cho users (all, specific, top buyers, top spenders)', 1);
    PRINT 'Permission VOUCHER_SEND created successfully';
END
ELSE
BEGIN
    PRINT 'Permission VOUCHER_SEND already exists';
END
GO

-- =============================================
-- Step 2: Add manager_staff_id to Voucher table
-- =============================================

-- Add manager_staff_id column to Voucher table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Voucher]') AND name = 'manager_staff_id')
BEGIN
    ALTER TABLE [dbo].[Voucher]
    ADD [manager_staff_id] INT NULL;
    PRINT 'Column manager_staff_id added to Voucher table successfully';
END
ELSE
BEGIN
    PRINT 'Column manager_staff_id already exists in Voucher table';
END
GO

-- Create Foreign Key from Voucher to ManagerStaff
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys
    WHERE name = 'FK_Voucher_ManagerStaff'
)
BEGIN
    ALTER TABLE [dbo].[Voucher]
    ADD CONSTRAINT [FK_Voucher_ManagerStaff]
    FOREIGN KEY ([manager_staff_id])
    REFERENCES [dbo].[ManagerStaff] ([manager_staff_id])
    ON DELETE NO ACTION;

    -- Create index on manager_staff_id for better query performance
    CREATE NONCLUSTERED INDEX [IX_Voucher_ManagerStaffId]
    ON [dbo].[Voucher] ([manager_staff_id])
    WHERE [manager_staff_id] IS NOT NULL;

    PRINT 'Foreign key FK_Voucher_ManagerStaff created successfully';
END
ELSE
BEGIN
    PRINT 'Foreign key FK_Voucher_ManagerStaff already exists';
END
GO

PRINT '=============================================';
PRINT 'Migration AddVoucherPermissionsAndManagerStaffId completed successfully!';
PRINT '=============================================';
PRINT 'NOTE: Voucher permissions are GLOBAL (no partnerId required)';
PRINT 'Only ONE ManagerStaff can have Voucher permissions at a time';
PRINT '=============================================';
GO

