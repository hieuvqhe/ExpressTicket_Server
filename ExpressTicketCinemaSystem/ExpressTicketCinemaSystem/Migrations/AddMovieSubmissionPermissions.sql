-- =============================================
-- Migration: Add Movie Submission Permissions for ManagerStaff
-- Description: Thêm các quyền quản lý phim (movie submission) cho ManagerStaff
-- Date: 2024-12-XX
-- =============================================

USE [ExpressTicketDB];
GO

PRINT '=============================================';
PRINT 'Starting migration: Add Movie Submission Permissions';
PRINT '=============================================';
GO

-- MOVIE_SUBMISSION Permissions
IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'MOVIE_SUBMISSION_READ')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description], [is_active]) VALUES
    ('MOVIE_SUBMISSION_READ', N'Xem yêu cầu duyệt phim', 'MOVIE_SUBMISSION', 'READ', N'Xem danh sách và chi tiết yêu cầu duyệt phim từ đối tác', 1);
    PRINT 'Permission MOVIE_SUBMISSION_READ created successfully';
END
ELSE
BEGIN
    PRINT 'Permission MOVIE_SUBMISSION_READ already exists';
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'MOVIE_SUBMISSION_APPROVE')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description], [is_active]) VALUES
    ('MOVIE_SUBMISSION_APPROVE', N'Duyệt yêu cầu phim', 'MOVIE_SUBMISSION', 'APPROVE', N'Duyệt yêu cầu duyệt phim từ đối tác. Tự động reject các phim trùng tiêu đề.', 1);
    PRINT 'Permission MOVIE_SUBMISSION_APPROVE created successfully';
END
ELSE
BEGIN
    PRINT 'Permission MOVIE_SUBMISSION_APPROVE already exists';
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Permissions] WHERE [permission_code] = 'MOVIE_SUBMISSION_REJECT')
BEGIN
    INSERT INTO [dbo].[Permissions] ([permission_code], [permission_name], [resource_type], [action_type], [description], [is_active]) VALUES
    ('MOVIE_SUBMISSION_REJECT', N'Từ chối yêu cầu phim', 'MOVIE_SUBMISSION', 'REJECT', N'Từ chối yêu cầu duyệt phim từ đối tác với lý do', 1);
    PRINT 'Permission MOVIE_SUBMISSION_REJECT created successfully';
END
ELSE
BEGIN
    PRINT 'Permission MOVIE_SUBMISSION_REJECT already exists';
END
GO

PRINT '=============================================';
PRINT 'Migration AddMovieSubmissionPermissions completed successfully!';
PRINT '=============================================';
GO








