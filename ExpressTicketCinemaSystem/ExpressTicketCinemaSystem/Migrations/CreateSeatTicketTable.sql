-- Create SeatTicket table for cashier check-in tracking
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SeatTicket]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SeatTicket] (
        [seat_ticket_id] INT IDENTITY(1,1) NOT NULL,
        [ticket_id] INT NOT NULL,
        [booking_id] INT NOT NULL,
        [seat_id] INT NOT NULL,
        [showtime_id] INT NOT NULL,
        [order_code] NVARCHAR(100) NOT NULL,
        [check_in_status] NVARCHAR(50) NULL, -- CHECKED_IN, NOT_CHECKED_IN, PARTIAL_CHECKED
        [check_in_time] DATETIME2(3) NULL,
        [checked_in_by] INT NULL, -- EmployeeId của cashier
        [cinema_id] INT NOT NULL,
        [created_at] DATETIME2(3) NOT NULL DEFAULT (sysutcdatetime()),
        [updated_at] DATETIME2(3) NULL,
        CONSTRAINT [PK__SeatTicket__D596F96BFAA74F8D] PRIMARY KEY CLUSTERED ([seat_ticket_id] ASC),
        CONSTRAINT [FK_SeatTicket_Ticket] FOREIGN KEY ([ticket_id]) REFERENCES [dbo].[Ticket] ([ticket_id]),
        CONSTRAINT [FK_SeatTicket_Booking] FOREIGN KEY ([booking_id]) REFERENCES [dbo].[Booking] ([booking_id]),
        CONSTRAINT [FK_SeatTicket_Seat] FOREIGN KEY ([seat_id]) REFERENCES [dbo].[Seat] ([seat_id]),
        CONSTRAINT [FK_SeatTicket_Showtime] FOREIGN KEY ([showtime_id]) REFERENCES [dbo].[Showtime] ([showtime_id]),
        CONSTRAINT [FK_SeatTicket_Cinema] FOREIGN KEY ([cinema_id]) REFERENCES [dbo].[Cinema] ([cinema_id]),
        CONSTRAINT [FK_SeatTicket_Employee] FOREIGN KEY ([checked_in_by]) REFERENCES [dbo].[Employee] ([employee_id])
    );

    -- Create indexes
    CREATE NONCLUSTERED INDEX [IX_SeatTicket_TicketId] ON [dbo].[SeatTicket] ([ticket_id]);
    CREATE NONCLUSTERED INDEX [IX_SeatTicket_Booking_Seat] ON [dbo].[SeatTicket] ([booking_id], [seat_id]);
    CREATE NONCLUSTERED INDEX [IX_SeatTicket_OrderCode_Seat] ON [dbo].[SeatTicket] ([order_code], [seat_id]);
    CREATE NONCLUSTERED INDEX [IX_SeatTicket_ShowtimeId] ON [dbo].[SeatTicket] ([showtime_id]);
    CREATE NONCLUSTERED INDEX [IX_SeatTicket_CheckInStatus] ON [dbo].[SeatTicket] ([check_in_status]) WHERE [check_in_status] = 'CHECKED_IN';
END
GO

