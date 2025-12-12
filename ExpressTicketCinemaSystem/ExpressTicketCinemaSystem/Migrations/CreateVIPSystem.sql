-- ============================================================
-- VIP MEMBERSHIP SYSTEM
-- ============================================================
-- Tạo bảng VIP Level để định nghĩa các cấp độ VIP
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VIPLevel]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[VIPLevel] (
        [vip_level_id] INT NOT NULL,
        [level_name] NVARCHAR(50) NOT NULL, -- VIP0, VIP1, VIP2, VIP3, VIP4
        [level_display_name] NVARCHAR(100) NOT NULL, -- "Thành viên", "Đồng", "Bạc", "Vàng", "Kim Cương"
        [min_points_required] INT NOT NULL, -- Điểm tối thiểu để đạt cấp độ này
        [point_earning_rate] DECIMAL(5,2) NOT NULL DEFAULT 1.00, -- Tỷ lệ tích điểm (1.00 = 1:1, 1.50 = 1.5x)
        [is_active] BIT NOT NULL DEFAULT 1,
        [created_at] DATETIME2(3) NOT NULL DEFAULT (sysutcdatetime()),
        [updated_at] DATETIME2(3) NULL,
        CONSTRAINT [PK_VIPLevel] PRIMARY KEY CLUSTERED ([vip_level_id] ASC)
    );

    -- Insert default VIP levels
    INSERT INTO [dbo].[VIPLevel] ([vip_level_id], [level_name], [level_display_name], [min_points_required], [point_earning_rate], [is_active])
    VALUES
        (0, N'VIP0', N'Thành viên', 0, 1.00, 1),
        (1, N'VIP1', N'Đồng', 1000, 1.10, 1),      -- 1.1x điểm từ 1000 điểm
        (2, N'VIP2', N'Bạc', 5000, 1.20, 1),       -- 1.2x điểm từ 5000 điểm
        (3, N'VIP3', N'Vàng', 20000, 1.30, 1),     -- 1.3x điểm từ 20000 điểm
        (4, N'VIP4', N'Kim Cương', 50000, 1.50, 1); -- 1.5x điểm từ 50000 điểm
END
GO

-- ============================================================
-- VIP Benefit - Định nghĩa các quyền lợi cho từng VIP level
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VIPBenefit]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[VIPBenefit] (
        [benefit_id] INT IDENTITY(1,1) NOT NULL,
        [vip_level_id] INT NOT NULL,
        [benefit_type] NVARCHAR(50) NOT NULL, -- UPGRADE_BONUS, BIRTHDAY_BONUS, DISCOUNT_VOUCHER, FREE_TICKET, PRIORITY_BOOKING, FREE_COMBO
        [benefit_name] NVARCHAR(200) NOT NULL,
        [benefit_description] NVARCHAR(500) NULL,
        [benefit_value] DECIMAL(10,2) NULL, -- Giá trị quyền lợi (ví dụ: 50000 cho upgrade bonus)
        [is_active] BIT NOT NULL DEFAULT 1,
        [created_at] DATETIME2(3) NOT NULL DEFAULT (sysutcdatetime()),
        [updated_at] DATETIME2(3) NULL,
        CONSTRAINT [PK_VIPBenefit] PRIMARY KEY CLUSTERED ([benefit_id] ASC),
        CONSTRAINT [FK_VIPBenefit_VIPLevel] FOREIGN KEY ([vip_level_id]) REFERENCES [dbo].[VIPLevel] ([vip_level_id])
    );

    -- Insert default benefits for each VIP level
    -- VIP0 (Thành viên) - Không có quyền lợi đặc biệt
    -- VIP1 (Đồng)
    INSERT INTO [dbo].[VIPBenefit] ([vip_level_id], [benefit_type], [benefit_name], [benefit_description], [benefit_value], [is_active])
    VALUES
        (1, N'UPGRADE_BONUS', N'Quà nâng cấp', N'Quà tặng khi nâng cấp lên VIP1', 50000, 1),
        (1, N'BIRTHDAY_BONUS', N'Quà sinh nhật', N'Quà tặng vào ngày sinh nhật', 50000, 1),
        (1, N'DISCOUNT_VOUCHER', N'Voucher giảm giá', N'Voucher giảm 5% cho mỗi đơn hàng', 5.00, 1);

    -- VIP2 (Bạc)
    INSERT INTO [dbo].[VIPBenefit] ([vip_level_id], [benefit_type], [benefit_name], [benefit_description], [benefit_value], [is_active])
    VALUES
        (2, N'UPGRADE_BONUS', N'Quà nâng cấp', N'Quà tặng khi nâng cấp lên VIP2', 100000, 1),
        (2, N'BIRTHDAY_BONUS', N'Quà sinh nhật', N'Quà tặng vào ngày sinh nhật', 100000, 1),
        (2, N'DISCOUNT_VOUCHER', N'Voucher giảm giá', N'Voucher giảm 10% cho mỗi đơn hàng', 10.00, 1),
        (2, N'FREE_COMBO', N'Combo miễn phí', N'1 combo miễn phí mỗi tháng', NULL, 1);

    -- VIP3 (Vàng)
    INSERT INTO [dbo].[VIPBenefit] ([vip_level_id], [benefit_type], [benefit_name], [benefit_description], [benefit_value], [is_active])
    VALUES
        (3, N'UPGRADE_BONUS', N'Quà nâng cấp', N'Quà tặng khi nâng cấp lên VIP3', 200000, 1),
        (3, N'BIRTHDAY_BONUS', N'Quà sinh nhật', N'Quà tặng vào ngày sinh nhật', 200000, 1),
        (3, N'DISCOUNT_VOUCHER', N'Voucher giảm giá', N'Voucher giảm 15% cho mỗi đơn hàng', 15.00, 1),
        (3, N'FREE_TICKET', N'Vé miễn phí', N'1 vé xem phim miễn phí mỗi tháng', NULL, 1),
        (3, N'PRIORITY_BOOKING', N'Ưu tiên đặt vé', N'Được ưu tiên đặt vé trước', NULL, 1);

    -- VIP4 (Kim Cương)
    INSERT INTO [dbo].[VIPBenefit] ([vip_level_id], [benefit_type], [benefit_name], [benefit_description], [benefit_value], [is_active])
    VALUES
        (4, N'UPGRADE_BONUS', N'Quà nâng cấp', N'Quà tặng khi nâng cấp lên VIP4', 500000, 1),
        (4, N'BIRTHDAY_BONUS', N'Quà sinh nhật', N'Quà tặng vào ngày sinh nhật', 500000, 1),
        (4, N'DISCOUNT_VOUCHER', N'Voucher giảm giá', N'Voucher giảm 20% cho mỗi đơn hàng', 20.00, 1),
        (4, N'FREE_TICKET', N'Vé miễn phí', N'2 vé xem phim miễn phí mỗi tháng', NULL, 1),
        (4, N'PRIORITY_BOOKING', N'Ưu tiên đặt vé', N'Được ưu tiên đặt vé trước', NULL, 1),
        (4, N'FREE_COMBO', N'Combo miễn phí', N'2 combo miễn phí mỗi tháng', NULL, 1);
END
GO

-- ============================================================
-- Tạo voucher cố định cho quà nâng cấp VIP
-- ============================================================
-- Lấy manager đầu tiên để tạo voucher (hoặc có thể dùng system admin)
DECLARE @ManagerId INT;
SELECT TOP 1 @ManagerId = [manager_id] FROM [dbo].[Manager] ORDER BY [manager_id];

IF @ManagerId IS NOT NULL
BEGIN
    -- Tạo voucher VIP1 (50k)
    IF NOT EXISTS (SELECT * FROM [dbo].[Voucher] WHERE [voucher_code] = 'VIP1')
    BEGIN
        INSERT INTO [dbo].[Voucher] ([voucher_code], [discount_type], [discount_val], [valid_from], [valid_to], [manager_id], [description], [is_active], [is_restricted], [created_at])
        VALUES (N'VIP1', N'fixed', 50000, CAST(GETDATE() AS DATE), DATEADD(YEAR, 10, CAST(GETDATE() AS DATE)), @ManagerId, N'Quà nâng cấp VIP1 - Đồng', 1, 1, GETDATE());
    END

    -- Tạo voucher VIP2 (100k)
    IF NOT EXISTS (SELECT * FROM [dbo].[Voucher] WHERE [voucher_code] = 'VIP2')
    BEGIN
        INSERT INTO [dbo].[Voucher] ([voucher_code], [discount_type], [discount_val], [valid_from], [valid_to], [manager_id], [description], [is_active], [is_restricted], [created_at])
        VALUES (N'VIP2', N'fixed', 100000, CAST(GETDATE() AS DATE), DATEADD(YEAR, 10, CAST(GETDATE() AS DATE)), @ManagerId, N'Quà nâng cấp VIP2 - Bạc', 1, 1, GETDATE());
    END

    -- Tạo voucher VIP3 (200k)
    IF NOT EXISTS (SELECT * FROM [dbo].[Voucher] WHERE [voucher_code] = 'VIP3')
    BEGIN
        INSERT INTO [dbo].[Voucher] ([voucher_code], [discount_type], [discount_val], [valid_from], [valid_to], [manager_id], [description], [is_active], [is_restricted], [created_at])
        VALUES (N'VIP3', N'fixed', 200000, CAST(GETDATE() AS DATE), DATEADD(YEAR, 10, CAST(GETDATE() AS DATE)), @ManagerId, N'Quà nâng cấp VIP3 - Vàng', 1, 1, GETDATE());
    END

    -- Tạo voucher VIP4 (500k)
    IF NOT EXISTS (SELECT * FROM [dbo].[Voucher] WHERE [voucher_code] = 'VIP4')
    BEGIN
        INSERT INTO [dbo].[Voucher] ([voucher_code], [discount_type], [discount_val], [valid_from], [valid_to], [manager_id], [description], [is_active], [is_restricted], [created_at])
        VALUES (N'VIP4', N'fixed', 500000, CAST(GETDATE() AS DATE), DATEADD(YEAR, 10, CAST(GETDATE() AS DATE)), @ManagerId, N'Quà nâng cấp VIP4 - Kim Cương', 1, 1, GETDATE());
    END
END
GO

-- ============================================================
-- VIP Member - Lưu thông tin VIP của từng customer
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VIPMember]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[VIPMember] (
        [vip_member_id] INT IDENTITY(1,1) NOT NULL,
        [customer_id] INT NOT NULL,
        [current_vip_level_id] INT NOT NULL DEFAULT 0,
        [total_points] INT NOT NULL DEFAULT 0, -- Tổng điểm tích lũy (không bao gồm điểm đã dùng)
        [growth_value] INT NOT NULL DEFAULT 0, -- Giá trị tăng trưởng (điểm tích lũy trong kỳ hiện tại để nâng cấp)
        [last_upgrade_date] DATETIME2(3) NULL,
        [birthday_bonus_claimed_year] INT NULL, -- Năm đã nhận quà sinh nhật gần nhất
        [monthly_free_ticket_claimed_month] INT NULL, -- Tháng đã nhận vé miễn phí gần nhất
        [monthly_free_combo_claimed_month] INT NULL, -- Tháng đã nhận combo miễn phí gần nhất
        [created_at] DATETIME2(3) NOT NULL DEFAULT (sysutcdatetime()),
        [updated_at] DATETIME2(3) NULL,
        CONSTRAINT [PK_VIPMember] PRIMARY KEY CLUSTERED ([vip_member_id] ASC),
        CONSTRAINT [FK_VIPMember_Customer] FOREIGN KEY ([customer_id]) REFERENCES [dbo].[Customer] ([customer_id]),
        CONSTRAINT [FK_VIPMember_VIPLevel] FOREIGN KEY ([current_vip_level_id]) REFERENCES [dbo].[VIPLevel] ([vip_level_id]),
        CONSTRAINT [UQ_VIPMember_Customer] UNIQUE ([customer_id])
    );

    CREATE NONCLUSTERED INDEX [IX_VIPMember_CustomerId] ON [dbo].[VIPMember] ([customer_id]);
    CREATE NONCLUSTERED INDEX [IX_VIPMember_VIPLevel] ON [dbo].[VIPMember] ([current_vip_level_id]);
END
GO

-- ============================================================
-- Point History - Lịch sử tích điểm và sử dụng điểm
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PointHistory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PointHistory] (
        [point_history_id] BIGINT IDENTITY(1,1) NOT NULL,
        [customer_id] INT NOT NULL,
        [order_id] NVARCHAR(100) NULL, -- OrderId nếu tích điểm từ đơn hàng
        [transaction_type] NVARCHAR(50) NOT NULL, -- EARNED, USED, EXPIRED, BONUS
        [points] INT NOT NULL, -- Số điểm (dương nếu EARNED/BONUS, âm nếu USED/EXPIRED)
        [description] NVARCHAR(500) NULL,
        [vip_level_id] INT NULL, -- VIP level tại thời điểm giao dịch
        [created_at] DATETIME2(3) NOT NULL DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_PointHistory] PRIMARY KEY CLUSTERED ([point_history_id] ASC),
        CONSTRAINT [FK_PointHistory_Customer] FOREIGN KEY ([customer_id]) REFERENCES [dbo].[Customer] ([customer_id]),
        CONSTRAINT [FK_PointHistory_VIPLevel] FOREIGN KEY ([vip_level_id]) REFERENCES [dbo].[VIPLevel] ([vip_level_id])
    );

    CREATE NONCLUSTERED INDEX [IX_PointHistory_CustomerId] ON [dbo].[PointHistory] ([customer_id]);
    CREATE NONCLUSTERED INDEX [IX_PointHistory_OrderId] ON [dbo].[PointHistory] ([order_id]);
    CREATE NONCLUSTERED INDEX [IX_PointHistory_CreatedAt] ON [dbo].[PointHistory] ([created_at]);
END
GO

-- ============================================================
-- VIP Benefit Claim - Lịch sử nhận quyền lợi
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[VIPBenefitClaim]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[VIPBenefitClaim] (
        [benefit_claim_id] BIGINT IDENTITY(1,1) NOT NULL,
        [vip_member_id] INT NOT NULL,
        [benefit_id] INT NOT NULL,
        [claim_type] NVARCHAR(50) NOT NULL, -- UPGRADE_BONUS, BIRTHDAY_BONUS, MONTHLY_FREE_TICKET, MONTHLY_FREE_COMBO
        [claim_value] DECIMAL(10,2) NULL,
        [voucher_id] INT NULL, -- Nếu quyền lợi là voucher, lưu voucher_id
        [status] NVARCHAR(50) NOT NULL DEFAULT N'PENDING', -- PENDING, CLAIMED, EXPIRED
        [claimed_at] DATETIME2(3) NULL,
        [expires_at] DATETIME2(3) NULL,
        [created_at] DATETIME2(3) NOT NULL DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_VIPBenefitClaim] PRIMARY KEY CLUSTERED ([benefit_claim_id] ASC),
        CONSTRAINT [FK_VIPBenefitClaim_VIPMember] FOREIGN KEY ([vip_member_id]) REFERENCES [dbo].[VIPMember] ([vip_member_id]),
        CONSTRAINT [FK_VIPBenefitClaim_VIPBenefit] FOREIGN KEY ([benefit_id]) REFERENCES [dbo].[VIPBenefit] ([benefit_id]),
        CONSTRAINT [FK_VIPBenefitClaim_Voucher] FOREIGN KEY ([voucher_id]) REFERENCES [dbo].[Voucher] ([voucher_id])
    );

    CREATE NONCLUSTERED INDEX [IX_VIPBenefitClaim_VIPMember] ON [dbo].[VIPBenefitClaim] ([vip_member_id]);
    CREATE NONCLUSTERED INDEX [IX_VIPBenefitClaim_Status] ON [dbo].[VIPBenefitClaim] ([status]);
END
GO

-- ============================================================
-- Cập nhật bảng Customer: Thêm cột VIP để tương thích ngược
-- (LoyaltyPoints đã có sẵn, nhưng sẽ dùng total_points từ VIPMember)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Customer]') AND name = 'LoyaltyPoints')
BEGIN
    -- Giữ nguyên cột LoyaltyPoints để tương thích ngược
    -- Sẽ đồng bộ với VIPMember.total_points
END
GO

