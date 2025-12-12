-- =============================================
-- Migration: Add manager_staff_id to MovieSubmission table
-- Description: Thêm trường manager_staff_id để track ManagerStaff đã approve/reject submission
-- Date: 2024-12-XX
-- =============================================

USE [ExpressTicketDB];
GO

PRINT '=============================================';
PRINT 'Starting migration: Add manager_staff_id to MovieSubmission';
PRINT '=============================================';
GO

-- Add manager_staff_id column to MovieSubmission table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[MovieSubmission]') AND name = 'manager_staff_id')
BEGIN
    ALTER TABLE [dbo].[MovieSubmission]
    ADD [manager_staff_id] INT NULL;
    PRINT 'Column manager_staff_id added to MovieSubmission table successfully';
END
ELSE
BEGIN
    PRINT 'Column manager_staff_id already exists in MovieSubmission table';
END
GO

-- Create Foreign Key from MovieSubmission to ManagerStaff
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys
    WHERE name = 'FK_MovieSubmission_ManagerStaff'
)
BEGIN
    ALTER TABLE [dbo].[MovieSubmission]
    ADD CONSTRAINT [FK_MovieSubmission_ManagerStaff]
    FOREIGN KEY ([manager_staff_id])
    REFERENCES [dbo].[ManagerStaff] ([manager_staff_id])
    ON DELETE NO ACTION;

    -- Create index on manager_staff_id for better query performance
    CREATE NONCLUSTERED INDEX [IX_MovieSubmission_ManagerStaffId]
    ON [dbo].[MovieSubmission] ([manager_staff_id])
    WHERE [manager_staff_id] IS NOT NULL;

    PRINT 'Foreign key FK_MovieSubmission_ManagerStaff created successfully';
END
ELSE
BEGIN
    PRINT 'Foreign key FK_MovieSubmission_ManagerStaff already exists';
END
GO

PRINT '=============================================';
PRINT 'Migration AddManagerStaffIdToMovieSubmission completed successfully!';
PRINT '=============================================';
GO








