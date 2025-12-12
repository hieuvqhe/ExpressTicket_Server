-- =============================================
-- Migration: Add ManagerStaff fields to Contract table
-- Description: Add fields to track which ManagerStaff signed the contract
-- Date: 2025-12-10
-- =============================================

USE [ExpressTicketDB];
GO

PRINT '=============================================';
PRINT 'Starting migration: Add ManagerStaff fields to Contract';
PRINT '=============================================';
GO

-- =============================================
-- Step 1: Add manager_staff_id column to Contract table
-- =============================================
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Contract]') 
    AND name = 'manager_staff_id'
)
BEGIN
    ALTER TABLE [dbo].[Contract]
    ADD [manager_staff_id] INT NULL;

    PRINT 'Column manager_staff_id added to Contract table successfully';
END
ELSE
BEGIN
    PRINT 'Column manager_staff_id already exists in Contract table';
END
GO

-- =============================================
-- Step 2: Add manager_staff_signature column to Contract table
-- =============================================
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Contract]') 
    AND name = 'manager_staff_signature'
)
BEGIN
    ALTER TABLE [dbo].[Contract]
    ADD [manager_staff_signature] NVARCHAR(500) NULL;

    PRINT 'Column manager_staff_signature added to Contract table successfully';
END
ELSE
BEGIN
    PRINT 'Column manager_staff_signature already exists in Contract table';
END
GO

-- =============================================
-- Step 3: Add manager_staff_signed_at column to Contract table
-- =============================================
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Contract]') 
    AND name = 'manager_staff_signed_at'
)
BEGIN
    ALTER TABLE [dbo].[Contract]
    ADD [manager_staff_signed_at] DATETIME NULL;

    PRINT 'Column manager_staff_signed_at added to Contract table successfully';
END
ELSE
BEGIN
    PRINT 'Column manager_staff_signed_at already exists in Contract table';
END
GO

-- =============================================
-- Step 4: Create Foreign Key from Contract to ManagerStaff
-- =============================================
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys 
    WHERE name = 'FK_Contract_ManagerStaff'
)
BEGIN
    ALTER TABLE [dbo].[Contract]
    ADD CONSTRAINT [FK_Contract_ManagerStaff]
    FOREIGN KEY ([manager_staff_id]) 
    REFERENCES [dbo].[ManagerStaff] ([manager_staff_id]) 
    ON DELETE NO ACTION;

    -- Create index on manager_staff_id for better query performance
    CREATE NONCLUSTERED INDEX [IX_Contract_ManagerStaffId]
    ON [dbo].[Contract] ([manager_staff_id])
    WHERE [manager_staff_id] IS NOT NULL;

    PRINT 'Foreign key FK_Contract_ManagerStaff created successfully';
END
ELSE
BEGIN
    PRINT 'Foreign key FK_Contract_ManagerStaff already exists';
END
GO

-- =============================================
-- Step 5: Add Extended Properties for documentation
-- =============================================
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Foreign key to ManagerStaff table. Indicates which ManagerStaff signed the contract temporarily (before Manager finalizes).', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Contract',
    @level2type = N'COLUMN', @level2name = N'manager_staff_id';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Digital signature of ManagerStaff when signing contract temporarily.', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Contract',
    @level2type = N'COLUMN', @level2name = N'manager_staff_signature';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Timestamp when ManagerStaff signed the contract temporarily.', 
    @level0type = N'SCHEMA', @level0name = N'dbo', 
    @level1type = N'TABLE', @level1name = N'Contract',
    @level2type = N'COLUMN', @level2name = N'manager_staff_signed_at';
GO

PRINT '=============================================';
PRINT 'Migration completed successfully!';
PRINT '=============================================';
GO








