-- Migration: Add IsRestricted field to Voucher table
-- Date: 2025-01-XX
-- Description: Thêm field IsRestricted để phân biệt Public voucher (ai cũng dùng được) và Restricted voucher (chỉ user được gửi mới dùng được)

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Voucher]') AND name = 'is_restricted')
BEGIN
    ALTER TABLE [dbo].[Voucher]
    ADD [is_restricted] BIT NOT NULL DEFAULT 0;

    PRINT 'Column is_restricted added to Voucher table successfully';
END
ELSE
BEGIN
    PRINT 'Column is_restricted already exists in Voucher table';
END
GO


















