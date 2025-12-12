-- Create EmployeeCinemaAssignment table for Staff-Cinema permission management
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeCinemaAssignment]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[EmployeeCinemaAssignment] (
        [assignment_id] INT IDENTITY(1,1) NOT NULL,
        [employee_id] INT NOT NULL,
        [cinema_id] INT NOT NULL,
        [assigned_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [assigned_by] INT NULL,
        [is_active] BIT NOT NULL DEFAULT 1,
        [unassigned_at] DATETIME2 NULL,
        CONSTRAINT [PK__EmployeeCinemaAssignment__AssignmentId] PRIMARY KEY ([assignment_id]),
        CONSTRAINT [FK_EmployeeCinemaAssignment_Employee] FOREIGN KEY ([employee_id]) REFERENCES [dbo].[Employee] ([employee_id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeCinemaAssignment_Cinema] FOREIGN KEY ([cinema_id]) REFERENCES [dbo].[Cinema] ([cinema_id]) ON DELETE NO ACTION
    );

    -- Create unique index to prevent duplicate assignments (same employee + same cinema)
    -- But allow one employee to manage multiple cinemas (1:N relationship)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_EmployeeCinemaAssignment_EmployeeId_CinemaId]
    ON [dbo].[EmployeeCinemaAssignment] ([employee_id], [cinema_id]);

    PRINT 'Table EmployeeCinemaAssignment created successfully';
END
ELSE
BEGIN
    PRINT 'Table EmployeeCinemaAssignment already exists';
END
GO

