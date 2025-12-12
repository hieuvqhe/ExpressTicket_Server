-- =============================================
-- Migration: Create ManagerStaff Permission System
-- Description: 
--   1. Create ManagerStaffPartnerPermissions table
--   2. Add ManagerStaff permissions to Permissions table
-- Date: 2025-01-XX
-- =============================================

-- =============================================
-- Step 1: Create ManagerStaffPartnerPermissions Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ManagerStaffPartnerPermissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ManagerStaffPartnerPermissions] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [manager_staff_id] INT NOT NULL,
        [partner_id] INT NULL,  -- NULL = áp dụng cho tất cả partners được assign
        [permission_id] INT NOT NULL,
        [granted_by] INT NOT NULL,  -- User ID của Manager cấp quyền
        [granted_at] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [is_active] BIT NOT NULL DEFAULT 1,
        [revoked_at] DATETIME2 NULL,
        [revoked_by] INT NULL,
        
        CONSTRAINT [PK_ManagerStaffPartnerPermissions] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_ManagerStaffPartnerPermissions_ManagerStaff] 
            FOREIGN KEY ([manager_staff_id]) REFERENCES [dbo].[ManagerStaff]([manager_staff_id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ManagerStaffPartnerPermissions_Partner] 
            FOREIGN KEY ([partner_id]) REFERENCES [dbo].[Partner]([partner_id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ManagerStaffPartnerPermissions_Permission] 
            FOREIGN KEY ([permission_id]) REFERENCES [dbo].[Permissions]([permission_id]),
        CONSTRAINT [FK_ManagerStaffPartnerPermissions_GrantedBy] 
            FOREIGN KEY ([granted_by]) REFERENCES [dbo].[Users]([user_id]),
        CONSTRAINT [FK_ManagerStaffPartnerPermissions_RevokedBy] 
            FOREIGN KEY ([revoked_by]) REFERENCES [dbo].[Users]([user_id])
    );
    
    -- Index để tăng tốc truy vấn phân quyền
    CREATE NONCLUSTERED INDEX [IX_ManagerStaffPartnerPermissions_Staff_Partner_Permission] 
        ON [dbo].[ManagerStaffPartnerPermissions]([manager_staff_id], [partner_id], [permission_id]) 
        WHERE [is_active] = 1;
    
    CREATE NONCLUSTERED INDEX [IX_ManagerStaffPartnerPermissions_Staff_Active] 
        ON [dbo].[ManagerStaffPartnerPermissions]([manager_staff_id]) 
        WHERE [is_active] = 1;
    
    PRINT 'Table ManagerStaffPartnerPermissions created successfully';
END
ELSE
BEGIN
    PRINT 'Table ManagerStaffPartnerPermissions already exists';
END
GO

-- =============================================
-- Step 2: Insert ManagerStaff Permissions (Seed Data)
-- =============================================
PRINT 'Inserting ManagerStaff permissions...';

-- PARTNER Permissions
IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'PARTNER_READ')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
    ('PARTNER_READ', N'Xem thông tin đối tác', 'PARTNER', 'READ', N'Xem thông tin chi tiết và danh sách đối tác được phân quyền');
END

IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'PARTNER_APPROVE')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
    ('PARTNER_APPROVE', N'Duyệt đối tác', 'PARTNER', 'APPROVE', N'Duyệt đơn đăng ký của đối tác');
END

IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'PARTNER_REJECT')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
    ('PARTNER_REJECT', N'Từ chối đối tác', 'PARTNER', 'REJECT', N'Từ chối đơn đăng ký của đối tác');
END

-- PARTNER_UPDATE removed - No API exists for updating partner information

-- CONTRACT Permissions
IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'CONTRACT_CREATE')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
    ('CONTRACT_CREATE', N'Tạo hợp đồng', 'CONTRACT', 'CREATE', N'Tạo hợp đồng mới với đối tác');
END

IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'CONTRACT_READ')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
    ('CONTRACT_READ', N'Xem hợp đồng', 'CONTRACT', 'READ', N'Xem thông tin chi tiết và danh sách hợp đồng');
END

IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'CONTRACT_UPDATE')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
    ('CONTRACT_UPDATE', N'Cập nhật hợp đồng', 'CONTRACT', 'UPDATE', N'Cập nhật thông tin hợp đồng');
END

IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'CONTRACT_SIGN_TEMPORARY')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
    ('CONTRACT_SIGN_TEMPORARY', N'Ký tạm hợp đồng', 'CONTRACT', 'SIGN_TEMPORARY', N'Ký tạm hợp đồng (chờ Manager finalize)');
END

IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'CONTRACT_FINALIZE')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
    ('CONTRACT_FINALIZE', N'Finalize hợp đồng', 'CONTRACT', 'FINALIZE', N'Finalize và khóa hợp đồng (chỉ Manager)');
END

IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'CONTRACT_SEND_PDF')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
    ('CONTRACT_SEND_PDF', N'Gửi hợp đồng PDF', 'CONTRACT', 'SEND_PDF', N'Gửi hợp đồng PDF cho đối tác để ký');
END

-- REPORT and BOOKING Permissions removed
-- These APIs are accessible to both Manager and ManagerStaff by default
-- The difference is in data scope:
--   - Manager: sees all data
--   - ManagerStaff: only sees data from assigned partners
-- No permission checking needed for these APIs

PRINT 'ManagerStaff permissions inserted successfully';
GO

-- =============================================
-- Step 3: Add comments/documentation
-- =============================================
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Table for ManagerStaff permissions on Partners. ManagerStaff can have permissions for specific partners or global permissions (partner_id = NULL).', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'ManagerStaffPartnerPermissions';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Foreign key to ManagerStaff table. Indicates which ManagerStaff this permission belongs to.', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'ManagerStaffPartnerPermissions',
    @level2type = N'COLUMN', @level2name = N'manager_staff_id';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Foreign key to Partner table. NULL means global permission (applies to all assigned partners).', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'ManagerStaffPartnerPermissions',
    @level2type = N'COLUMN', @level2name = N'partner_id';
GO

PRINT '=============================================';
PRINT 'ManagerStaff Permission System Migration completed successfully!';
PRINT '=============================================';
GO

