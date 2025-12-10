-- Migration: Create VoucherReservation table to reserve voucher for booking session
-- Date: 2025-01-XX
-- Description: Tạo bảng VoucherReservation để reserve voucher cho session, tránh race condition khi nhiều user cùng dùng voucher

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VoucherReservation')
BEGIN
    CREATE TABLE [dbo].[VoucherReservation] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [voucher_id] INT NOT NULL,
        [session_id] UNIQUEIDENTIFIER NOT NULL,
        [user_id] INT NOT NULL,
        [reserved_at] DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        [expires_at] DATETIME2(3) NOT NULL,
        [released_at] DATETIME2(3) NULL,
        CONSTRAINT [PK_VoucherReservation] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [FK_VoucherReservation_Voucher] FOREIGN KEY ([voucher_id]) REFERENCES [dbo].[Voucher] ([voucher_id]) ON DELETE CASCADE,
        CONSTRAINT [FK_VoucherReservation_BookingSession] FOREIGN KEY ([session_id]) REFERENCES [dbo].[booking_sessions] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_VoucherReservation_User] FOREIGN KEY ([user_id]) REFERENCES [dbo].[User] ([user_id]) ON DELETE NO ACTION
    );

    -- Tạo unique index để đảm bảo mỗi voucher chỉ được reserve bởi 1 session tại 1 thời điểm
    -- Chỉ áp dụng cho các reservation còn hiệu lực (released_at IS NULL và expires_at > NOW)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_VoucherReservation_Voucher_Active]
        ON [dbo].[VoucherReservation]([voucher_id] ASC)
        WHERE [released_at] IS NULL;

    -- Index cho session_id để query nhanh khi cleanup
    CREATE NONCLUSTERED INDEX [IX_VoucherReservation_Session]
        ON [dbo].[VoucherReservation]([session_id] ASC);

    -- Index cho expires_at để cleanup expired reservations
    CREATE NONCLUSTERED INDEX [IX_VoucherReservation_Expires]
        ON [dbo].[VoucherReservation]([expires_at] ASC)
        WHERE [released_at] IS NULL;

    PRINT 'Table VoucherReservation created successfully';
END
ELSE
BEGIN
    PRINT 'Table VoucherReservation already exists';
END
GO


















