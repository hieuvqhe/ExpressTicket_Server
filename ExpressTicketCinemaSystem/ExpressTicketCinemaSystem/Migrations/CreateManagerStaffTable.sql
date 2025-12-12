-- =============================================
-- Migration: Create ManagerStaff Table and Add ManagerStaffId to Partner
-- Description: 
--   1. Create ManagerStaff table for manager staff management
--   2. Add manager_staff_id column to Partner table
--   3. Create foreign keys and constraints
-- Date: 2025-01-XX
-- =============================================

-- =============================================
-- Step 1: Create ManagerStaff Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ManagerStaff]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ManagerStaff] (
        [manager_staff_id] INT IDENTITY(1,1) NOT NULL,
        [manager_id] INT NOT NULL,
        [user_id] INT NOT NULL,
        [full_name] NVARCHAR(255) NOT NULL,
        [role_type] VARCHAR(50) NOT NULL,
        [hire_date] DATE NOT NULL,
        [is_active] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [PK__ManagerStaff__ManagerStaffId] PRIMARY KEY ([manager_staff_id]),
        CONSTRAINT [FK_ManagerStaff_Manager] FOREIGN KEY ([manager_id]) REFERENCES [dbo].[Manager] ([manager_id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ManagerStaff_User] FOREIGN KEY ([user_id]) REFERENCES [dbo].[User] ([user_id]) ON DELETE NO ACTION
    );

    -- Create unique index on user_id to ensure one user can only be one ManagerStaff
    CREATE UNIQUE NONCLUSTERED INDEX [UQ__ManagerStaff__B9BE370E]
    ON [dbo].[ManagerStaff] ([user_id]);

    -- Create index on manager_id for better query performance
    CREATE NONCLUSTERED INDEX [IX_ManagerStaff_ManagerId]
    ON [dbo].[ManagerStaff] ([manager_id]);

    -- Create index on is_active for filtering active staff
    CREATE NONCLUSTERED INDEX [IX_ManagerStaff_IsActive]
    ON [dbo].[ManagerStaff] ([is_active])
    WHERE [is_active] = 1;

    PRINT 'Table ManagerStaff created successfully';
END
ELSE
BEGIN
    PRINT 'Table ManagerStaff already exists';
END
GO

-- =============================================
-- Step 2: Add manager_staff_id column to Partner table
-- =============================================
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Partner]') 
    AND name = 'manager_staff_id'
)
BEGIN
    ALTER TABLE [dbo].[Partner]
    ADD [manager_staff_id] INT NULL;

    PRINT 'Column manager_staff_id added to Partner table successfully';
END
ELSE
BEGIN
    PRINT 'Column manager_staff_id already exists in Partner table';
END
GO

-- =============================================
-- Step 3: Create Foreign Key from Partner to ManagerStaff
-- =============================================
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys 
    WHERE name = 'FK_Partner_ManagerStaff'
)
BEGIN
    ALTER TABLE [dbo].[Partner]
    ADD CONSTRAINT [FK_Partner_ManagerStaff]
    FOREIGN KEY ([manager_staff_id]) 
    REFERENCES [dbo].[ManagerStaff] ([manager_staff_id]) 
    ON DELETE NO ACTION;

    -- Create index on manager_staff_id for better query performance
    CREATE NONCLUSTERED INDEX [IX_Partner_ManagerStaffId]
    ON [dbo].[Partner] ([manager_staff_id])
    WHERE [manager_staff_id] IS NOT NULL;

    PRINT 'Foreign key FK_Partner_ManagerStaff created successfully';
END
ELSE
BEGIN
    PRINT 'Foreign key FK_Partner_ManagerStaff already exists';
END
GO

-- =============================================
-- Step 4: Add comments/documentation
-- =============================================
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Table for manager staff accounts. ManagerStaff are staff members created by Manager to help manage partners.', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'ManagerStaff';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Foreign key to Manager table. Indicates which Manager this staff belongs to.', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'ManagerStaff',
    @level2type = N'COLUMN', @level2name = N'manager_id';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Foreign key to User table. Links to the user account for this ManagerStaff.', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'ManagerStaff',
    @level2type = N'COLUMN', @level2name = N'user_id';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Role type for ManagerStaff. Typically "ManagerStaff".', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'ManagerStaff',
    @level2type = N'COLUMN', @level2name = N'role_type';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Foreign key to ManagerStaff table. Indicates which ManagerStaff is assigned to manage this partner.', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Partner',
    @level2type = N'COLUMN', @level2name = N'manager_staff_id';
GO

PRINT '=============================================';
PRINT 'Migration completed successfully!';
PRINT '=============================================';
GO








