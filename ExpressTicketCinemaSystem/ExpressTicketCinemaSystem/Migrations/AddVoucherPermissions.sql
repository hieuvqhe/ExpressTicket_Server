-- =============================================
-- Migration: Add Voucher Permissions for ManagerStaff
-- Description: Thêm các quyền quản lý voucher cho ManagerStaff (không cần partnerId)
-- Date: 2024-12-XX
-- =============================================

USE [ExpressTicketDB];
GO

PRINT '=============================================';
PRINT 'Starting migration: Add Voucher Permissions';
PRINT '=============================================';
GO

-- VOUCHER Permissions (không cần partnerId - global permission)
IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'VOUCHER_CREATE')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description], [is_active]) VALUES
    ('VOUCHER_CREATE', N'Tạo voucher', 'VOUCHER', 'CREATE', N'Tạo voucher mới cho người dùng', 1);
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
    ('VOUCHER_SEND', N'Gửi voucher', 'VOUCHER', 'SEND', N'Gửi voucher cho người dùng (all users, specific users, top buyers, top spenders)', 1);
    PRINT 'Permission VOUCHER_SEND created successfully';
END
ELSE
BEGIN
    PRINT 'Permission VOUCHER_SEND already exists';
END
GO

PRINT '=============================================';
PRINT 'Migration AddVoucherPermissions completed successfully!';
PRINT '=============================================';
GO








