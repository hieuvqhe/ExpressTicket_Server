IF COL_LENGTH('AuditLog', 'role') IS NULL
BEGIN
    ALTER TABLE AuditLog ADD role VARCHAR(32) NULL;
END;

IF COL_LENGTH('AuditLog', 'before_data') IS NULL
BEGIN
    ALTER TABLE AuditLog ADD before_data NVARCHAR(MAX) NULL;
END;

IF COL_LENGTH('AuditLog', 'after_data') IS NULL
BEGIN
    ALTER TABLE AuditLog ADD after_data NVARCHAR(MAX) NULL;
END;

IF COL_LENGTH('AuditLog', 'metadata') IS NULL
BEGIN
    ALTER TABLE AuditLog ADD metadata NVARCHAR(MAX) NULL;
END;

IF COL_LENGTH('AuditLog', 'ip_address') IS NULL
BEGIN
    ALTER TABLE AuditLog ADD ip_address VARCHAR(64) NULL;
END;

IF COL_LENGTH('AuditLog', 'user_agent') IS NULL
BEGIN
    ALTER TABLE AuditLog ADD user_agent VARCHAR(256) NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_AuditLog_Table_Record_Timestamp'
      AND object_id = OBJECT_ID('AuditLog')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditLog_Table_Record_Timestamp
        ON AuditLog (table_name, record_id, timestamp DESC);
END;























