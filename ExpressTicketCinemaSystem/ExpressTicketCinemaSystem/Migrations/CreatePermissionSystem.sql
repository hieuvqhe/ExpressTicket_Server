-- =============================================
-- Migration: Create Permission System
-- Description: Tạo hệ thống phân quyền chi tiết cho Staff
-- Author: System
-- Date: 2025-11-27
-- =============================================

-- =============================================
-- 1. Tạo bảng Permissions
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Permissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Permissions] (
        [permission_id] INT IDENTITY(1,1) NOT NULL,
        [permission_code] NVARCHAR(100) NOT NULL,
        [permission_name] NVARCHAR(255) NOT NULL,
        [resource_type] NVARCHAR(50) NOT NULL,  -- SCREEN, SEAT_TYPE, SEAT_LAYOUT, SHOWTIME, CINEMA, SERVICE
        [action_type] NVARCHAR(50) NOT NULL,    -- CREATE, READ, UPDATE, DELETE, BULK_CREATE, BULK_DELETE
        [description] NVARCHAR(500) NULL,
        [is_active] BIT NOT NULL DEFAULT 1,
        [created_at] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        
        CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED ([permission_id] ASC),
        CONSTRAINT [UQ_Permissions_Code] UNIQUE ([permission_code])
    );
    
    PRINT 'Table Permissions created successfully';
END
ELSE
BEGIN
    PRINT 'Table Permissions already exists';
END
GO

-- =============================================
-- 2. Tạo bảng EmployeeCinemaPermissions
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeCinemaPermissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[EmployeeCinemaPermissions] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [employee_id] INT NOT NULL,
        [cinema_id] INT NULL,  -- NULL = áp dụng cho tất cả cinemas của employee
        [permission_id] INT NOT NULL,
        [granted_by] INT NOT NULL,  -- User ID của Partner cấp quyền
        [granted_at] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [is_active] BIT NOT NULL DEFAULT 1,
        [revoked_at] DATETIME2 NULL,
        [revoked_by] INT NULL,
        
        CONSTRAINT [PK_EmployeeCinemaPermissions] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_EmployeeCinemaPermissions_Employee] 
            FOREIGN KEY ([employee_id]) REFERENCES [dbo].[Employee]([employee_id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EmployeeCinemaPermissions_Cinema] 
            FOREIGN KEY ([cinema_id]) REFERENCES [dbo].[Cinema]([cinema_id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EmployeeCinemaPermissions_Permission] 
            FOREIGN KEY ([permission_id]) REFERENCES [dbo].[Permissions]([permission_id]),
        CONSTRAINT [FK_EmployeeCinemaPermissions_GrantedBy] 
            FOREIGN KEY ([granted_by]) REFERENCES [dbo].[Users]([user_id])
    );
    
    -- Index để tăng tốc truy vấn phân quyền
    CREATE NONCLUSTERED INDEX [IX_EmployeeCinemaPermissions_Employee_Cinema_Permission] 
        ON [dbo].[EmployeeCinemaPermissions]([employee_id], [cinema_id], [permission_id]) 
        WHERE [is_active] = 1;
    
    CREATE NONCLUSTERED INDEX [IX_EmployeeCinemaPermissions_Employee_Active] 
        ON [dbo].[EmployeeCinemaPermissions]([employee_id]) 
        WHERE [is_active] = 1;
    
    PRINT 'Table EmployeeCinemaPermissions created successfully';
END
ELSE
BEGIN
    PRINT 'Table EmployeeCinemaPermissions already exists';
END
GO

-- =============================================
-- 3. Insert Default Permissions (Seed Data)
-- =============================================
PRINT 'Inserting default permissions...';

-- CINEMA Permissions
INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
('CINEMA_READ', N'Xem thông tin rạp', 'CINEMA', 'READ', N'Xem thông tin chi tiết và danh sách rạp được phân quyền'),
('CINEMA_UPDATE', N'Cập nhật thông tin rạp', 'CINEMA', 'UPDATE', N'Cập nhật thông tin rạp (tên, địa chỉ, hotline, v.v.)'),
('CINEMA_DELETE', N'Xóa rạp', 'CINEMA', 'DELETE', N'Xóa (deactivate) rạp chiếu');

-- SCREEN Permissions
INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
('SCREEN_CREATE', N'Tạo phòng chiếu', 'SCREEN', 'CREATE', N'Tạo phòng chiếu mới trong rạp'),
('SCREEN_READ', N'Xem thông tin phòng chiếu', 'SCREEN', 'READ', N'Xem thông tin chi tiết và danh sách phòng chiếu'),
('SCREEN_UPDATE', N'Cập nhật phòng chiếu', 'SCREEN', 'UPDATE', N'Cập nhật thông tin phòng chiếu'),
('SCREEN_DELETE', N'Xóa phòng chiếu', 'SCREEN', 'DELETE', N'Xóa phòng chiếu');

-- SEAT_TYPE Permissions
INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
('SEAT_TYPE_CREATE', N'Tạo loại ghế', 'SEAT_TYPE', 'CREATE', N'Tạo loại ghế mới (VIP, Standard, Couple, v.v.)'),
('SEAT_TYPE_READ', N'Xem loại ghế', 'SEAT_TYPE', 'READ', N'Xem thông tin và danh sách loại ghế'),
('SEAT_TYPE_UPDATE', N'Cập nhật loại ghế', 'SEAT_TYPE', 'UPDATE', N'Cập nhật thông tin loại ghế'),
('SEAT_TYPE_DELETE', N'Xóa loại ghế', 'SEAT_TYPE', 'DELETE', N'Xóa loại ghế');

-- SEAT_LAYOUT Permissions
INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
('SEAT_LAYOUT_CREATE', N'Tạo sơ đồ ghế', 'SEAT_LAYOUT', 'CREATE', N'Tạo ghế đơn lẻ trong sơ đồ phòng chiếu'),
('SEAT_LAYOUT_READ', N'Xem sơ đồ ghế', 'SEAT_LAYOUT', 'READ', N'Xem sơ đồ ghế của phòng chiếu'),
('SEAT_LAYOUT_UPDATE', N'Cập nhật sơ đồ ghế', 'SEAT_LAYOUT', 'UPDATE', N'Cập nhật vị trí, loại ghế trong sơ đồ'),
('SEAT_LAYOUT_DELETE', N'Xóa ghế', 'SEAT_LAYOUT', 'DELETE', N'Xóa ghế trong sơ đồ'),
('SEAT_LAYOUT_BULK_CREATE', N'Tạo hàng loạt ghế', 'SEAT_LAYOUT', 'BULK_CREATE', N'Tạo nhiều ghế cùng lúc (bulk)'),
('SEAT_LAYOUT_BULK_DELETE', N'Xóa hàng loạt ghế', 'SEAT_LAYOUT', 'BULK_DELETE', N'Xóa nhiều ghế cùng lúc (bulk)');

-- SHOWTIME Permissions
INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
('SHOWTIME_CREATE', N'Tạo suất chiếu', 'SHOWTIME', 'CREATE', N'Tạo suất chiếu mới'),
('SHOWTIME_READ', N'Xem suất chiếu', 'SHOWTIME', 'READ', N'Xem thông tin và danh sách suất chiếu'),
('SHOWTIME_UPDATE', N'Cập nhật suất chiếu', 'SHOWTIME', 'UPDATE', N'Cập nhật thông tin suất chiếu'),
('SHOWTIME_DELETE', N'Xóa suất chiếu', 'SHOWTIME', 'DELETE', N'Xóa suất chiếu');

-- SERVICE (Combo) Permissions
INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
('SERVICE_READ', N'Xem dịch vụ/combo', 'SERVICE', 'READ', N'Xem danh sách và chi tiết dịch vụ/combo');

-- BOOKING Permissions (read-only for staff)
INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description]) VALUES
('BOOKING_READ', N'Xem đơn đặt vé', 'BOOKING', 'READ', N'Xem danh sách và chi tiết đơn đặt vé'),
('BOOKING_STATISTICS', N'Xem thống kê booking', 'BOOKING', 'READ', N'Xem thống kê về đơn đặt vé');

PRINT 'Default permissions inserted successfully';
GO

-- =============================================
-- 4. Tạo View để query permissions dễ dàng
-- =============================================
IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[vw_EmployeePermissions]'))
    DROP VIEW [dbo].[vw_EmployeePermissions];
GO

CREATE VIEW [dbo].[vw_EmployeePermissions] AS
SELECT 
    ecp.id,
    ecp.employee_id,
    e.full_name AS employee_name,
    e.partner_id,
    ecp.cinema_id,
    c.cinema_name,
    ecp.permission_id,
    p.permission_code,
    p.permission_name,
    p.resource_type,
    p.action_type,
    p.description AS permission_description,
    ecp.granted_by,
    u.fullname AS granted_by_name,
    ecp.granted_at,
    ecp.is_active,
    ecp.revoked_at,
    ecp.revoked_by
FROM [dbo].[EmployeeCinemaPermissions] ecp
INNER JOIN [dbo].[Employee] e ON ecp.employee_id = e.employee_id
LEFT JOIN [dbo].[Cinema] c ON ecp.cinema_id = c.cinema_id
INNER JOIN [dbo].[Permissions] p ON ecp.permission_id = p.permission_id
INNER JOIN [dbo].[Users] u ON ecp.granted_by = u.user_id
WHERE ecp.is_active = 1;
GO

PRINT 'View vw_EmployeePermissions created successfully';
GO

-- =============================================
-- 5. Tạo Stored Procedure để check permission
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE object_id = OBJECT_ID(N'[dbo].[sp_CheckEmployeePermission]'))
    DROP PROCEDURE [dbo].[sp_CheckEmployeePermission];
GO

CREATE PROCEDURE [dbo].[sp_CheckEmployeePermission]
    @EmployeeId INT,
    @CinemaId INT,
    @PermissionCode NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @HasPermission BIT = 0;
    
    -- Check if employee has permission for specific cinema OR for all cinemas (cinema_id IS NULL)
    IF EXISTS (
        SELECT 1 
        FROM [dbo].[EmployeeCinemaPermissions] ecp
        INNER JOIN [dbo].[Permissions] p ON ecp.permission_id = p.permission_id
        WHERE ecp.employee_id = @EmployeeId
            AND (ecp.cinema_id = @CinemaId OR ecp.cinema_id IS NULL)
            AND p.permission_code = @PermissionCode
            AND ecp.is_active = 1
            AND p.is_active = 1
    )
    BEGIN
        SET @HasPermission = 1;
    END
    
    SELECT @HasPermission AS HasPermission;
END
GO

PRINT 'Stored Procedure sp_CheckEmployeePermission created successfully';
GO

PRINT '==========================================';
PRINT 'Permission System Migration Completed!';
PRINT '==========================================';
GO




