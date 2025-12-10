-- Migration: Create UserVoucher table to track voucher usage per user
-- Date: 2025-01-XX
-- Description: Tạo bảng UserVoucher để đảm bảo mỗi user chỉ sử dụng voucher 1 lần

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserVoucher')
BEGIN
    CREATE TABLE [dbo].[UserVoucher] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [voucher_id] INT NOT NULL,
        [user_id] INT NOT NULL,
        [is_used] BIT NOT NULL DEFAULT 0,
        [used_at] DATETIME2(3) NULL,
        [booking_id] INT NULL,
        [created_at] DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_UserVoucher] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_UserVoucher_Voucher] FOREIGN KEY ([voucher_id]) REFERENCES [dbo].[Voucher] ([voucher_id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserVoucher_User] FOREIGN KEY ([user_id]) REFERENCES [dbo].[User] ([user_id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserVoucher_Booking] FOREIGN KEY ([booking_id]) REFERENCES [dbo].[Booking] ([booking_id]) ON DELETE SET NULL
    );

    -- Tạo unique index để đảm bảo mỗi user chỉ có 1 record cho mỗi voucher
    CREATE UNIQUE NONCLUSTERED INDEX [IX_UserVoucher_Voucher_User]
        ON [dbo].[UserVoucher]([voucher_id] ASC, [user_id] ASC);

    -- Tạo index cho booking_id để query nhanh hơn
    CREATE NONCLUSTERED INDEX [IX_UserVoucher_Booking]
        ON [dbo].[UserVoucher]([booking_id] ASC)
        WHERE [booking_id] IS NOT NULL;

    PRINT 'Table UserVoucher created successfully';
END
ELSE
BEGIN
    PRINT 'Table UserVoucher already exists';
END
GO


















