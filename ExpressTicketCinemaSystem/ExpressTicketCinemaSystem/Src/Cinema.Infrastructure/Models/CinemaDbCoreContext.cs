using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class CinemaDbCoreContext : DbContext
{
    public CinemaDbCoreContext()
    {
    }

    public CinemaDbCoreContext(DbContextOptions<CinemaDbCoreContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Actor> Actors { get; set; }

    public virtual DbSet<Advertisement> Advertisements { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingSession> BookingSessions { get; set; }

    public virtual DbSet<Cinema> Cinemas { get; set; }

    public virtual DbSet<Contract> Contracts { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<EmailChangeRequest> EmailChangeRequests { get; set; }

    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EmployeeCinemaAssignment> EmployeeCinemaAssignments { get; set; }

    public virtual DbSet<EmployeeCinemaPermission> EmployeeCinemaPermissions { get; set; }

    public virtual DbSet<GameShow> GameShows { get; set; }

    public virtual DbSet<Manager> Managers { get; set; }

    public virtual DbSet<Movie> Movies { get; set; }

    public virtual DbSet<MovieActor> MovieActors { get; set; }
    public virtual DbSet<MovieSubmission> MovieSubmissions { get; set; }

    public virtual DbSet<MovieSubmissionActor> MovieSubmissionActors { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Partner> Partners { get; set; }

    public virtual DbSet<PartnerReport> PartnerReports { get; set; }

    public virtual DbSet<PasswordResetCode> PasswordResetCodes { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<RatingFilm> RatingFilms { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<RevenueReport> RevenueReports { get; set; }

    public virtual DbSet<Screen> Screens { get; set; }

    public virtual DbSet<Seat> Seats { get; set; }

    public virtual DbSet<SeatLock> SeatLocks { get; set; }

    public virtual DbSet<SeatMap> SeatMaps { get; set; }

    public virtual DbSet<SeatType> SeatTypes { get; set; }

    public virtual DbSet<SeatTicket> SeatTickets { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ServiceOrder> ServiceOrders { get; set; }

    public virtual DbSet<Showtime> Showtimes { get; set; }

    public virtual DbSet<SupportTicket> SupportTickets { get; set; }

    public virtual DbSet<SystemAdmin> SystemAdmins { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<VoucherEmailHistory> VoucherEmailHistories { get; set; }

    public virtual DbSet<UserVoucher> UserVouchers { get; set; }

    public virtual DbSet<VoucherReservation> VoucherReservations { get; set; }

    public virtual DbSet<VIPLevel> VIPLevels { get; set; }

    public virtual DbSet<VIPBenefit> VIPBenefits { get; set; }

    public virtual DbSet<VIPMember> VIPMembers { get; set; }

    public virtual DbSet<PointHistory> PointHistories { get; set; }

    public virtual DbSet<VIPBenefitClaim> VIPBenefitClaims { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Actor>(entity =>
        {
            entity.HasKey(e => e.ActorId).HasName("PK__Actor__8B2447B407266A7B");

            entity.ToTable("Actor");

            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Advertisement>(entity =>
        {
            entity.HasKey(e => e.AdId).HasName("PK__Advertis__CAA4A627FD83E869");

            entity.ToTable("Advertisement");

            entity.Property(e => e.AdId).HasColumnName("ad_id");
            entity.Property(e => e.AdTitle)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ad_title");
            entity.Property(e => e.AdType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ad_type");
            entity.Property(e => e.Cost)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("cost");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.PartnerId).HasColumnName("partner_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Partner).WithMany(p => p.Advertisements)
                .HasForeignKey(d => d.PartnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ad_Partner");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__AuditLog__9E2397E03CC26928");

            entity.ToTable("AuditLog");

            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("action");
            entity.Property(e => e.Role)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("role");
            entity.Property(e => e.RecordId).HasColumnName("record_id");
            entity.Property(e => e.TableName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("table_name");
            entity.Property(e => e.BeforeData)
                .HasColumnName("before_data");
            entity.Property(e => e.AfterData)
                .HasColumnName("after_data");
            entity.Property(e => e.Metadata)
                .HasColumnName("metadata");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("ip_address");
            entity.Property(e => e.UserAgent)
                .HasMaxLength(256)
                .IsUnicode(false)
                .HasColumnName("user_agent");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("timestamp");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Booking__5DE3A5B1B58620C7");

            entity.ToTable("Booking");

            entity.HasIndex(e => e.State, "IX_booking_state");

            entity.HasIndex(e => e.PaymentTxId, "IX_booking_tx");

            entity.HasIndex(e => e.BookingCode, "UQ__Booking__FF29040FBC07FE7F").IsUnique();

            entity.HasIndex(e => e.OrderCode, "UX_booking_order_code")
                .IsUnique()
                .HasFilter("([order_code] IS NOT NULL)");

            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.BookingCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("booking_code");
            entity.Property(e => e.BookingTime)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("booking_time");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.OrderCode)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("order_code");
            entity.Property(e => e.PaymentProvider)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("payment_provider");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(24)
                .IsUnicode(false)
                .HasColumnName("payment_status");
            entity.Property(e => e.PaymentTxId)
                .HasMaxLength(128)
                .IsUnicode(false)
                .HasColumnName("payment_tx_id");
            entity.Property(e => e.PricingSnapshot).HasColumnName("pricing_snapshot");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.ShowtimeId).HasColumnName("showtime_id");
            entity.Property(e => e.State)
                .HasMaxLength(24)
                .IsUnicode(false)
                .HasDefaultValue("PENDING_PAYMENT")
                .HasColumnName("state");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");
            entity.Property(e => e.VoucherId).HasColumnName("voucher_id");

            entity.HasOne(d => d.Customer).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Booking_Customer");

            entity.HasOne(d => d.Showtime).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ShowtimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Booking_Showtime");

            entity.HasOne(d => d.Voucher).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("FK_Booking_Voucher");
        });

        modelBuilder.Entity<BookingSession>(entity =>
        {
            entity.ToTable("booking_sessions");

            entity.HasIndex(e => e.ExpiresAt, "IX_bs_expires");

            entity.HasIndex(e => e.ShowtimeId, "IX_bs_showtime");

            entity.HasIndex(e => e.UserId, "IX_bs_user");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.CouponCode)
                .HasMaxLength(64)
                .HasColumnName("coupon_code");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasPrecision(3)
                .HasColumnName("expires_at");
            entity.Property(e => e.ItemsJson)
                .HasDefaultValue("{\"seats\":[],\"combos\":[]}")
                .HasColumnName("items_json");
            entity.Property(e => e.PricingJson)
                .HasDefaultValue("{\"subtotal\":0,\"discount\":0,\"fees\":0,\"total\":0,\"currency\":\"VND\"}")
                .HasColumnName("pricing_json");
            entity.Property(e => e.ShowtimeId).HasColumnName("showtime_id");
            entity.Property(e => e.State)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("DRAFT")
                .HasColumnName("state");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Version)
                .HasDefaultValue(1)
                .HasColumnName("version")
                .IsConcurrencyToken(); // Optimistic locking: đảm bảo không bị lost update khi có concurrent requests

            entity.HasOne(d => d.Showtime).WithMany(p => p.BookingSessions)
                .HasForeignKey(d => d.ShowtimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bs_showtime");
        });

        modelBuilder.Entity<Cinema>(entity =>
        {
            entity.HasKey(e => e.CinemaId).HasName("PK__Cinema__56628778C5A53926");

            entity.ToTable("Cinema");

            entity.Property(e => e.CinemaId).HasColumnName("cinema_id");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.CinemaName)
                .HasMaxLength(255)
                .HasColumnName("cinema_name");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.District)
                .HasMaxLength(100)
                .HasColumnName("district");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Latitude)
                .HasColumnType("decimal(10, 8)")
                .HasColumnName("latitude");
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(500)
                .HasColumnName("logo_url");
            entity.Property(e => e.Longitude)
                .HasColumnType("decimal(11, 8)")
                .HasColumnName("longitude");
            entity.Property(e => e.PartnerId).HasColumnName("partner_id");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Partner).WithMany(p => p.Cinemas)
                .HasForeignKey(d => d.PartnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cinema_Partner");
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("PK__Contract__F8D66423D7306F3B");

            entity.ToTable("Contract");

            entity.HasIndex(e => e.ContractNumber, "UQ__Contract__1CA37CCEA61FF260").IsUnique();

            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.CommissionRate)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("commission_rate");
            entity.Property(e => e.ContractHash)
                .HasMaxLength(500)
                .HasColumnName("contract_hash");
            entity.Property(e => e.ContractNumber)
                .HasMaxLength(50)
                .HasColumnName("contract_number");
            entity.Property(e => e.ContractType)
                .HasMaxLength(50)
                .HasDefaultValue("partnership")
                .HasColumnName("contract_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsLocked).HasColumnName("is_locked");
            entity.Property(e => e.LockedAt).HasColumnName("locked_at");
            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
            entity.Property(e => e.ManagerSignature)
                .HasMaxLength(500)
                .HasColumnName("manager_signature");
            entity.Property(e => e.ManagerSignedAt).HasColumnName("manager_signed_at");
            entity.Property(e => e.MinimumRevenue)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("minimum_revenue");
            entity.Property(e => e.PartnerId).HasColumnName("partner_id");
            entity.Property(e => e.PartnerSignatureUrl)
                .HasMaxLength(500)
                .HasColumnName("partner_signature_url");
            entity.Property(e => e.PartnerSignedAt).HasColumnName("partner_signed_at");
            entity.Property(e => e.PdfUrl).HasMaxLength(500);
            entity.Property(e => e.SignedAt).HasColumnName("signed_at");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("draft")
                .HasColumnName("status");
            entity.Property(e => e.TermsAndConditions).IsUnicode(true).HasColumnName("terms_and_conditions");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(true)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Manager).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Contract_Manager");

            entity.HasOne(d => d.Partner).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.PartnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Contract_Partner");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__CD65CB85DFE42CAD");

            entity.ToTable("Customer");

            entity.HasIndex(e => e.UserId, "UQ__Customer__B9BE370E16E6F77E").IsUnique();

            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(true)
                .HasColumnName("address");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("gender");
            entity.Property(e => e.LoyaltyPoints).HasColumnName("loyalty_points");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Customer)
                .HasForeignKey<Customer>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Customer_User");
        });

        modelBuilder.Entity<EmailChangeRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__EmailCha__33A8517A7627A013");

            entity.Property(e => e.RequestId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CurrentCodeHash).HasMaxLength(64);
            entity.Property(e => e.NewCodeHash).HasMaxLength(64);
            entity.Property(e => e.NewEmail).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.EmailChangeRequests)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmailChangeRequests_User");
        });

        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EmailVer__3213E83F5D7BEED1");

            entity.ToTable("EmailVerificationToken");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConsumedAt).HasColumnName("consumed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(64)
                .HasColumnName("token_hash");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.EmailVerificationTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmailVerificationToken_User");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__C52E0BA867C01783");

            entity.ToTable("Employee");

            entity.HasIndex(e => e.UserId, "UQ__Employee__B9BE370E1BB01CC2").IsUnique();

            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .IsUnicode(true)
                .HasColumnName("full_name");
            entity.Property(e => e.HireDate).HasColumnName("hire_date");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PartnerId).HasColumnName("partner_id");
            entity.Property(e => e.RoleType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("role_type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Partner).WithMany(p => p.Employees)
                .HasForeignKey(d => d.PartnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employee_Partner");

            entity.HasOne(d => d.User).WithOne(p => p.Employee)
                .HasForeignKey<Employee>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employee_User");
        });

        modelBuilder.Entity<EmployeeCinemaAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentId).HasName("PK__EmployeeCinemaAssignment__AssignmentId");

            entity.ToTable("EmployeeCinemaAssignment");

            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.CinemaId).HasColumnName("cinema_id");
            entity.Property(e => e.AssignedAt).HasColumnName("assigned_at");
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.UnassignedAt).HasColumnName("unassigned_at");

            entity.HasOne(d => d.Employee).WithMany(p => p.CinemaAssignments)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeCinemaAssignment_Employee");

            entity.HasOne(d => d.Cinema).WithMany(p => p.EmployeeAssignments)
                .HasForeignKey(d => d.CinemaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeCinemaAssignment_Cinema");

            entity.HasOne(d => d.AssignedByUser)
                .WithMany()
                .HasForeignKey(d => d.AssignedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeCinemaAssignment_AssignedByUser");

            // Unique constraint: một employee không thể được gán trùng lặp cùng một cinema
            // Nhưng một employee có thể quản lý nhiều cinema khác nhau (1:N relationship)
            entity.HasIndex(e => new { e.EmployeeId, e.CinemaId })
                .IsUnique();
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("PK_Permissions");

            entity.ToTable("Permissions");

            entity.HasIndex(e => e.PermissionCode, "UQ_Permissions_Code").IsUnique();

            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.PermissionCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("permission_code");
            entity.Property(e => e.PermissionName)
                .HasMaxLength(255)
                .IsUnicode(true)
                .HasColumnName("permission_name");
            entity.Property(e => e.ResourceType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("resource_type");
            entity.Property(e => e.ActionType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("action_type");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(true)
                .HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
        });

        modelBuilder.Entity<EmployeeCinemaPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_EmployeeCinemaPermissions");

            entity.ToTable("EmployeeCinemaPermissions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.CinemaId).HasColumnName("cinema_id");
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.GrantedBy).HasColumnName("granted_by");
            entity.Property(e => e.GrantedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("granted_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.RevokedBy).HasColumnName("revoked_by");

            entity.HasOne(d => d.Employee).WithMany()
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_EmployeeCinemaPermissions_Employee");

            entity.HasOne(d => d.Cinema).WithMany()
                .HasForeignKey(d => d.CinemaId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_EmployeeCinemaPermissions_Cinema");

            entity.HasOne(d => d.Permission).WithMany(p => p.EmployeeCinemaPermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeCinemaPermissions_Permission");

            entity.HasOne(d => d.GrantedByUser).WithMany()
                .HasForeignKey(d => d.GrantedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeCinemaPermissions_GrantedBy");

            entity.HasOne(d => d.RevokedByUser).WithMany()
                .HasForeignKey(d => d.RevokedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeCinemaPermissions_RevokedBy");

            // Index để tăng tốc truy vấn
            entity.HasIndex(e => new { e.EmployeeId, e.CinemaId, e.PermissionId })
                .HasDatabaseName("IX_EmployeeCinemaPermissions_Employee_Cinema_Permission")
                .HasFilter("[is_active] = 1");

            entity.HasIndex(e => e.EmployeeId)
                .HasDatabaseName("IX_EmployeeCinemaPermissions_Employee_Active")
                .HasFilter("[is_active] = 1");
        });

        modelBuilder.Entity<GameShow>(entity =>
        {
            entity.HasKey(e => e.GameshowId).HasName("PK__GameShow__651C4D02FE1105CE");

            entity.ToTable("GameShow");

            entity.Property(e => e.GameshowId).HasColumnName("gameshow_id");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .IsUnicode(true)
                .HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.PartnerId).HasColumnName("partner_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("title");

            entity.HasOne(d => d.Partner).WithMany(p => p.GameShows)
                .HasForeignKey(d => d.PartnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GameShow_Partner");
        });

        modelBuilder.Entity<Manager>(entity =>
        {
            entity.HasKey(e => e.ManagerId).HasName("PK__Manager__5A6073FC38BBE20D");

            entity.ToTable("Manager");

            entity.HasIndex(e => e.UserId, "UQ__Manager__B9BE370EE822C7B9").IsUnique();

            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
            entity.Property(e => e.Department)
                .HasMaxLength(100)
                .IsUnicode(true)
                .HasColumnName("department");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .IsUnicode(true)
                .HasColumnName("full_name");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Manager)
                .HasForeignKey<Manager>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Manager_User");
        });

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasKey(e => e.MovieId).HasName("PK__Movie__83CDF7494F73DF92");

            entity.ToTable("Movie");

            entity.Property(e => e.MovieId).HasColumnName("movie_id");
            entity.Property(e => e.AverageRating)
                .HasColumnType("decimal(3, 1)")
                .HasColumnName("average_rating");
            entity.Property(e => e.BannerUrl)
               .HasMaxLength(500)
               .HasColumnName("banner_url");
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .IsUnicode(true)  // NVARCHAR để hỗ trợ tiếng Việt
                .HasColumnName("country");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(256)
                .HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Director)
                .HasMaxLength(255)
                .IsUnicode(true)  // NVARCHAR để hỗ trợ tiếng Việt
                .HasColumnName("director");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.EndDate).HasColumnName("endDate");
            entity.Property(e => e.Genre)
                .HasMaxLength(100)
                .IsUnicode(true)  // NVARCHAR để hỗ trợ tiếng Việt
                .HasColumnName("genre");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Language)
                .HasMaxLength(50)
                .IsUnicode(true)  // NVARCHAR để hỗ trợ tiếng Việt
                .HasColumnName("language");
            entity.Property(e => e.PartnerId).HasColumnName("partner_id");
            entity.Property(e => e.PosterUrl).HasColumnName("poster_url");
            entity.Property(e => e.PremiereDate).HasColumnName("premiereDate");
            entity.Property(e => e.Production)
                .HasMaxLength(255)
                .HasColumnName("production");
            entity.Property(e => e.RatingsCount).HasColumnName("ratings_count");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(true)  // NVARCHAR để hỗ trợ tiếng Việt
                .HasColumnName("title");
            entity.Property(e => e.TrailerUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("trailer_url");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasOne(d => d.Partner).WithMany(p => p.Movies)
                .HasForeignKey(d => d.PartnerId)
                .HasConstraintName("FK_Movies_Partner");
        });

        modelBuilder.Entity<MovieActor>(entity =>
        {
            entity.HasKey(e => new { e.MovieId, e.ActorId }).HasName("PK__MovieAct__DB7FB332E1E3B37C");

            entity.ToTable("MovieActor");

            entity.Property(e => e.MovieId).HasColumnName("movie_id");
            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.Role)
                .HasMaxLength(255)
                .HasColumnName("role");

            entity.HasOne(d => d.Actor).WithMany(p => p.MovieActors)
                .HasForeignKey(d => d.ActorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MovieActo__actor__22751F6C");

            entity.HasOne(d => d.Movie).WithMany(p => p.MovieActors)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MovieActo__movie__236943A5");
        });

        modelBuilder.Entity<MovieSubmission>(entity =>
        {
            entity.HasKey(e => e.MovieSubmissionId).HasName("PK__MovieSub__59F97B714EE23744");

            entity.ToTable("MovieSubmission");

            entity.HasIndex(e => e.MovieId, "IX_MovieSubmissions_MovieId");

            entity.HasIndex(e => e.PartnerId, "IX_MovieSubmissions_PartnerId");

            entity.HasIndex(e => e.Status, "IX_MovieSubmissions_Status");

            entity.HasIndex(e => new { e.Status, e.Title }, "IX_Moviesubmission_status_title");

            entity.Property(e => e.MovieSubmissionId).HasColumnName("movie_submission_id");
            entity.Property(e => e.AdditionalNotes)
                .HasMaxLength(1000)
                .HasColumnName("additional_notes");
            entity.Property(e => e.BannerUrl)
                .HasMaxLength(500)
                .HasColumnName("banner_url");
            entity.Property(e => e.CopyrightDocumentUrl)
                .HasMaxLength(500)
                .HasColumnName("copyright_document_url");
            entity.Property(e => e.Country)
                .HasMaxLength(50)
                .HasColumnName("country");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(2000)
                .HasColumnName("description");
            entity.Property(e => e.Director)
                .HasMaxLength(100)
                .HasColumnName("director");
            entity.Property(e => e.DistributionLicenseUrl)
                .HasMaxLength(500)
                .HasColumnName("distribution_license_url");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Genre)
                .HasMaxLength(100)
                .HasColumnName("genre");
            entity.Property(e => e.Language)
                .HasMaxLength(50)
                .HasColumnName("language");
            entity.Property(e => e.MovieId).HasColumnName("movie_id");
            entity.Property(e => e.PartnerId).HasColumnName("partner_id");
            entity.Property(e => e.PosterUrl)
                .HasMaxLength(500)
                .HasColumnName("poster_url");
            entity.Property(e => e.PremiereDate).HasColumnName("premiere_date");
            entity.Property(e => e.Production)
                .HasMaxLength(200)
                .HasColumnName("production");
            entity.Property(e => e.RejectionReason)
                .HasMaxLength(1000)
                .HasColumnName("rejection_reason");
            entity.Property(e => e.ResubmitCount).HasColumnName("resubmit_count");
            entity.Property(e => e.ResubmittedAt)
                .HasColumnType("datetime")
                .HasColumnName("resubmitted_at");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewerId).HasColumnName("reviewer_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Draft")
                .HasColumnName("status");
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.TrailerUrl)
                .HasMaxLength(500)
                .HasColumnName("trailer_url");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Movie).WithMany(p => p.MovieSubmissions)
                .HasForeignKey(d => d.MovieId)
                .HasConstraintName("FK_MovieSubmission_Movie");

            entity.HasOne(d => d.Partner).WithMany(p => p.MovieSubmissions)
                .HasForeignKey(d => d.PartnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovieSubmission_Partners");

            entity.HasOne(d => d.Reviewer).WithMany(p => p.MovieSubmissions)
                .HasForeignKey(d => d.ReviewerId)
                .HasConstraintName("FK_MovieSubmission_Reviewer");
        });
        modelBuilder.Entity<MovieSubmissionActor>(entity =>
        {
            entity.HasKey(e => e.MovieSubmissionActorId)
                  .HasName("PK__MovieSub__7A6D1DE67875FD24");
            entity.Property(e => e.MovieSubmissionActorId)
                  .HasColumnName("movie_submission_actor_id");

            entity.Property(e => e.MovieSubmissionId)
                  .HasColumnName("movie_submission_id");
            entity.Property(e => e.ActorId)
                  .HasColumnName("actor_id")
                  .IsRequired(false);
            entity.Property(e => e.ActorName)
                  .HasMaxLength(100)
                  .IsRequired()                      
                  .HasColumnName("actor_name");
            entity.Property(e => e.ActorAvatarUrl)
                  .HasMaxLength(512)                  
                  .HasColumnName("actor_avatar_url");
            entity.Property(e => e.Role)
                  .HasMaxLength(100)
                  .IsRequired()
                  .HasDefaultValueSql("N'Diễn viên'")
                  .HasColumnName("role");
            entity.HasOne(d => d.Actor)
                  .WithMany(p => p.MovieSubmissionActors)
                  .HasForeignKey(d => d.ActorId)
                  .OnDelete(DeleteBehavior.SetNull)       
                  .IsRequired(false)
                  .HasConstraintName("FK_MovieSubmissionActors_Actor");
            entity.HasOne(d => d.MovieSubmission)
                  .WithMany(p => p.MovieSubmissionActors)
                  .HasForeignKey(d => d.MovieSubmissionId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .IsRequired()
                  .HasConstraintName("FK_MovieSubmissionActors_Submission");
        });

        modelBuilder.Entity<Partner>(entity =>
        {
            entity.HasKey(e => e.PartnerId).HasName("PK__Partner__576F1B2707E11F04");

            entity.ToTable("Partner");

            entity.HasIndex(e => e.UserId, "UQ__Partner__B9BE370EDBC635FC").IsUnique();

            entity.Property(e => e.PartnerId).HasColumnName("partner_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(true)
                .HasColumnName("address");
            entity.Property(e => e.ApprovedAt)
                .HasColumnType("datetime")
                .HasColumnName("approved_at");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.BusinessRegistrationCertificateUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("business_registration_certificate_url");
            entity.Property(e => e.CommissionRate)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("commission_rate");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.IdentityCardUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("identity_card_url");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
            entity.Property(e => e.PartnerName)
                .HasMaxLength(255)
                .IsUnicode(true)
                .HasColumnName("partner_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.RejectionReason)
                .HasColumnType("text")
                .HasColumnName("rejection_reason");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(true)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.TaxCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("tax_code");
            entity.Property(e => e.TaxRegistrationCertificateUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("tax_registration_certificate_url");
            entity.Property(e => e.TheaterPhotosUrl)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("theater_photos_url");
            entity.Property(e => e.AdditionalDocumentsUrl)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("additional_documents_url");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Manager).WithMany(p => p.Partners)
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Partner_Manager");

            entity.HasOne(d => d.User).WithOne(p => p.Partner)
                .HasForeignKey<Partner>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Partner_User");
        });

        modelBuilder.Entity<PartnerReport>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__PartnerR__779B7C58A966EF11");

            entity.ToTable("PartnerReport");

            entity.Property(e => e.ReportId).HasColumnName("report_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.NetRevenue)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("net_revenue");
            entity.Property(e => e.PartnerId).HasColumnName("partner_id");
            entity.Property(e => e.ReportDate).HasColumnName("report_date");
            entity.Property(e => e.TotalRevenue)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("total_revenue");

            entity.HasOne(d => d.Partner).WithMany(p => p.PartnerReports)
                .HasForeignKey(d => d.PartnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PartnerReport_Partner");
        });

        modelBuilder.Entity<PasswordResetCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Password__3214EC07765EA35D");

            entity.ToTable("PasswordResetCode");

            entity.Property(e => e.Code).HasMaxLength(10);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpiredAt).HasColumnType("datetime");
            entity.Property(e => e.VerifiedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetCodes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PasswordResetCode_User");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__ED1FC9EA873B7337");

            entity.ToTable("Payment");

            entity.HasIndex(e => e.BookingId, "IX_payment_booking");

            entity.HasIndex(e => e.BookingId, "UQ__Payment__5DE3A5B0CD07FA15").IsUnique();

            entity.HasIndex(e => e.TransactionId, "UX_payment_tx")
                .IsUnique()
                .HasFilter("([transaction_id] IS NOT NULL)");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Method)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("method");
            entity.Property(e => e.PaidAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("paid_at");
            entity.Property(e => e.PayloadJson).HasColumnName("payload_json");
            entity.Property(e => e.Provider)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasColumnName("provider");
            entity.Property(e => e.SignatureOk).HasColumnName("signature_ok");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TransactionId)
                .HasMaxLength(128)
                .IsUnicode(false)
                .HasColumnName("transaction_id");

            entity.HasOne(d => d.Booking).WithOne(p => p.Payment)
                .HasForeignKey<Payment>(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Booking");
        });

        modelBuilder.Entity<RatingFilm>(entity =>
        {
            entity.HasKey(e => e.RatingId).HasName("PK__RatingFi__D35B278B70F5ADCF");

            entity.ToTable("RatingFilm");

            entity.Property(e => e.RatingId).HasColumnName("rating_id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.MovieId).HasColumnName("movie_id");
            entity.Property(e => e.RatingAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("rating_at");
            entity.Property(e => e.RatingStar).HasColumnName("rating_star");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.IsDeleted)
                .HasColumnName("is_deleted")
                .HasDefaultValue(false);
            entity.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at")
                .IsRequired(false);
            entity.Property(e => e.ImageUrls)
                .HasColumnName("image_urls")
                .IsRequired(false);

            entity.HasOne(d => d.Movie).WithMany(p => p.RatingFilms)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RatingFilm_Movie");

            entity.HasOne(d => d.User).WithMany(p => p.RatingFilms)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RatingFilm_User");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC0744503447");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Token).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_RefreshTokens_Users");
        });

        modelBuilder.Entity<RevenueReport>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__RevenueR__779B7C58C02AA36F");

            entity.ToTable("RevenueReport");

            entity.Property(e => e.ReportId).HasColumnName("report_id");
            entity.Property(e => e.CinemaId).HasColumnName("cinema_id");
            entity.Property(e => e.GeneratedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("generated_at");
            entity.Property(e => e.OccupancyRate)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("occupancy_rate");
            entity.Property(e => e.ReportDate).HasColumnName("report_date");
            entity.Property(e => e.ServiceRevenue)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("service_revenue");
            entity.Property(e => e.TicketRevenue)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("ticket_revenue");
            entity.Property(e => e.TotalTickets).HasColumnName("total_tickets");

            entity.HasOne(d => d.Cinema).WithMany(p => p.RevenueReports)
                .HasForeignKey(d => d.CinemaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RevenueReport_Cinema");
        });

        modelBuilder.Entity<Screen>(entity =>
        {
            entity.HasKey(e => e.ScreenId).HasName("PK__Screen__CC19B67AF5A247A8");

            entity.ToTable("Screen");
            entity.Property(e => e.ScreenId).HasColumnName("screen_id");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.CinemaId).HasColumnName("cinema_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("created_date");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.ScreenName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("screen_name");
            entity.Property(e => e.ScreenType)
                .HasMaxLength(50)
                .HasColumnName("screen_type");
            entity.Property(e => e.SeatColumns).HasColumnName("seat_columns");
            entity.Property(e => e.SeatRows).HasColumnName("seat_rows");
            entity.Property(e => e.SoundSystem)
                .HasMaxLength(100)
                .HasColumnName("sound_system");
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("updated_date");

            entity.HasOne(d => d.Cinema).WithMany(p => p.Screens)
                .HasForeignKey(d => d.CinemaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Screen_Cinema");
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(e => e.SeatId).HasName("PK__Seat__906DED9CC5ACC0E6");

            entity.ToTable("Seat");

            entity.HasIndex(e => new { e.ScreenId, e.RowCode, e.SeatNumber }, "UQ_Seat").IsUnique();

            entity.Property(e => e.SeatId).HasColumnName("seat_id");
            entity.Property(e => e.RowCode)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("row_code");
            entity.Property(e => e.ScreenId).HasColumnName("screen_id");
            entity.Property(e => e.SeatName)
                .HasMaxLength(100)
                .HasColumnName("seat_name");
            entity.Property(e => e.SeatNumber).HasColumnName("seat_number");
            entity.Property(e => e.SeatTypeId).HasColumnName("seat_type_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Screen).WithMany(p => p.Seats)
                .HasForeignKey(d => d.ScreenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Seat_Screen");
            entity.HasOne(d => d.SeatType).WithMany(p => p.Seats)
                .HasForeignKey(d => d.SeatTypeId)
                .HasConstraintName("FK_Seat_SeatType");
        });

        modelBuilder.Entity<SeatLock>(entity =>
        {
            entity.HasKey(e => new { e.ShowtimeId, e.SeatId });

            entity.ToTable("seat_locks");

            entity.HasIndex(e => e.LockedBySession, "IX_sl_session");

            entity.HasIndex(e => e.LockedUntil, "IX_sl_until");

            entity.Property(e => e.ShowtimeId).HasColumnName("showtime_id");
            entity.Property(e => e.SeatId).HasColumnName("seat_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.LockedBySession).HasColumnName("locked_by_session");
            entity.Property(e => e.LockedUntil)
                .HasPrecision(3)
                .HasColumnName("locked_until");

            entity.HasOne(d => d.LockedBySessionNavigation).WithMany(p => p.SeatLocks)
                .HasForeignKey(d => d.LockedBySession)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sl_session");

            entity.HasOne(d => d.Seat).WithMany(p => p.SeatLocks)
                .HasForeignKey(d => d.SeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sl_seat");
        });

        modelBuilder.Entity<SeatMap>(entity =>
        {
            entity.HasKey(e => e.SeatMapId).HasName("PK__SeatMap__55CFDE03A2A6F0A8");

            entity.ToTable("SeatMap");
            entity.HasIndex(e => e.ScreenId, "UQ_SeatMap_Screen").IsUnique();

            entity.HasIndex(e => e.ScreenId, "UQ__SeatMap__CC19B67B52ED2058").IsUnique();

            entity.Property(e => e.SeatMapId).HasColumnName("seat_map_id");
            entity.Property(e => e.LayoutData).HasColumnName("layout_data");
            entity.Property(e => e.ScreenId).HasColumnName("screen_id");
            entity.Property(e => e.TotalColumns)
               .HasDefaultValue(15)
               .HasColumnName("total_columns");
            entity.Property(e => e.TotalRows)
                .HasDefaultValue(10)
                .HasColumnName("total_rows");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Screen).WithOne(p => p.SeatMap)
                .HasForeignKey<SeatMap>(d => d.ScreenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SeatMap_Screen");
        });
        modelBuilder.Entity<SeatType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SeatType__3214EC07FFC567B5");

            entity.ToTable("SeatType");

            entity.HasIndex(e => e.Code, "UQ__SeatType__A25C5AA7FF7BA3B3").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Color)
                .HasMaxLength(7)
                .HasDefaultValue("#CCCCCC");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.PartnerId)
               .HasDefaultValue(7)
               .HasColumnName("partner_id");
            entity.Property(e => e.Status).HasDefaultValue(true);
            entity.Property(e => e.Surcharge).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Partner).WithMany(p => p.SeatTypes)
               .HasForeignKey(d => d.PartnerId)
               .OnDelete(DeleteBehavior.ClientSetNull)
               .HasConstraintName("FK_SeatType_Partner");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Service__3E0DB8AF2C0E5A4F");

            entity.ToTable("Service");

            entity.HasIndex(e => new { e.PartnerId, e.IsAvailable }, "IX_Service_Partner_Available");

            entity.HasIndex(e => new { e.PartnerId, e.Code }, "UX_Service_Partner_Code").IsUnique();

            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("image_url");
            entity.Property(e => e.IsAvailable)
                .HasDefaultValue(true)
                .HasColumnName("is_available");
            entity.Property(e => e.PartnerId).HasColumnName("partner_id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ServiceName)
                .HasMaxLength(255)
                .IsUnicode(true)
                .HasColumnName("service_name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Partner).WithMany(p => p.Services)
                .HasForeignKey(d => d.PartnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Service_Partner");
        });

        modelBuilder.Entity<ServiceOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__ServiceO__46596229618EB124");

            entity.ToTable("ServiceOrder");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Booking).WithMany(p => p.ServiceOrders)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceOrder_Booking");

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceOrders)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceOrder_Service");
        });

        modelBuilder.Entity<Showtime>(entity =>
        {
            entity.HasKey(e => e.ShowtimeId).HasName("PK__Showtime__A406B518317E5754");

            entity.ToTable("Showtime");

            entity.Property(e => e.ShowtimeId).HasColumnName("showtime_id");
            entity.Property(e => e.AvailableSeats).HasColumnName("available_seats");
            entity.Property(e => e.BasePrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("base_price");
            entity.Property(e => e.CinemaId).HasColumnName("cinema_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.FormatType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("format_type");
            entity.Property(e => e.MovieId).HasColumnName("movie_id");
            entity.Property(e => e.ScreenId).HasColumnName("screen_id");
            entity.Property(e => e.ShowDatetime).HasColumnName("show_datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            // Các trường mới được thêm
            entity.Property(e => e.EndTime)
                .HasColumnName("end_time")
                .IsRequired(false);

            entity.Property(e => e.AvailableSeats)
                .HasColumnName("available_seats")
                .IsRequired(false);

            entity.Property(e => e.FormatType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("format_type")
                .IsRequired(false);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired(false);

            entity.HasOne(d => d.Cinema).WithMany(p => p.Showtimes)
                .HasForeignKey(d => d.CinemaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Showtime_Cinema");

            entity.HasOne(d => d.Movie).WithMany(p => p.Showtimes)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Showtime_Movie");

            entity.HasOne(d => d.Screen).WithMany(p => p.Showtimes)
                .HasForeignKey(d => d.ScreenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Showtime_Screen");
        });

        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasKey(e => e.TicketId).HasName("PK__SupportT__D596F96B86DB12AA");

            entity.ToTable("SupportTicket");

            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.Subject)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("subject");
        });

        modelBuilder.Entity<SystemAdmin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK__SystemAd__43AA41410CFD6CB3");

            entity.ToTable("SystemAdmin");

            entity.HasIndex(e => e.UserId, "UQ__SystemAd__B9BE370E34CD7A79").IsUnique();

            entity.Property(e => e.AdminId).HasColumnName("admin_id");
            entity.Property(e => e.AdminLevel)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("admin_level");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Permissions).HasColumnName("permissions");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.SystemAdmin)
                .HasForeignKey<SystemAdmin>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SystemAdmin_User");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.TicketId).HasName("PK__Ticket__D596F96BFAA74F8C");

            entity.ToTable("Ticket");

            entity.HasIndex(e => new { e.ShowtimeId, e.SeatId }, "UQ_Ticket").IsUnique();

            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.SeatId).HasColumnName("seat_id");
            entity.Property(e => e.ShowtimeId).HasColumnName("showtime_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Booking).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ticket_Booking");

            entity.HasOne(d => d.Seat).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.SeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ticket_Seat");

            entity.HasOne(d => d.Showtime).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.ShowtimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ticket_Showtime");
        });

        modelBuilder.Entity<SeatTicket>(entity =>
        {
            entity.HasKey(e => e.SeatTicketId).HasName("PK__SeatTicket__D596F96BFAA74F8D");

            entity.ToTable("SeatTicket");

            entity.HasIndex(e => new { e.TicketId }, "IX_SeatTicket_TicketId");
            entity.HasIndex(e => new { e.BookingId, e.SeatId }, "IX_SeatTicket_Booking_Seat");
            entity.HasIndex(e => new { e.OrderCode, e.SeatId }, "IX_SeatTicket_OrderCode_Seat");

            entity.Property(e => e.SeatTicketId).HasColumnName("seat_ticket_id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.SeatId).HasColumnName("seat_id");
            entity.Property(e => e.ShowtimeId).HasColumnName("showtime_id");
            entity.Property(e => e.OrderCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("order_code");
            entity.Property(e => e.CheckInStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("check_in_status");
            entity.Property(e => e.CheckInTime)
                .HasPrecision(3)
                .HasColumnName("check_in_time");
            entity.Property(e => e.CheckedInBy).HasColumnName("checked_in_by");
            entity.Property(e => e.CinemaId).HasColumnName("cinema_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Ticket).WithMany()
                .HasForeignKey(d => d.TicketId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SeatTicket_Ticket");

            entity.HasOne(d => d.Booking).WithMany()
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SeatTicket_Booking");

            entity.HasOne(d => d.Seat).WithMany()
                .HasForeignKey(d => d.SeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SeatTicket_Seat");

            entity.HasOne(d => d.Showtime).WithMany()
                .HasForeignKey(d => d.ShowtimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SeatTicket_Showtime");

            entity.HasOne(d => d.Cinema).WithMany()
                .HasForeignKey(d => d.CinemaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SeatTicket_Cinema");

            entity.HasOne(d => d.CheckedInByEmployee).WithMany()
                .HasForeignKey(d => d.CheckedInBy)
                .HasConstraintName("FK_SeatTicket_Employee");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__B9BE370F2B516448");

            entity.ToTable("User");

            entity.HasIndex(e => e.Username, "UQ__User__536C85E4FE0D5744").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__User__AB6E616487E088E3").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasDefaultValue("https://tse2.mm.bing.net/th/id/OIP.Ai9h_6D7ojZdsZnE4_6SDgAAAA?cb=12&rs=1&pid=ImgDetMain&o=7&rm=3");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.EmailConfirmed).HasColumnName("email_confirmed");
            entity.Property(e => e.Fullname)
                .HasMaxLength(255)
                .IsUnicode(true)
                .HasColumnName("fullname");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.UserType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("user_type");
            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt)
       .HasColumnType("datetime")
       .HasColumnName("updated_at")
       .IsRequired(false);

            entity.Property(e => e.IsBanned)
                .HasColumnType("bit")
                .HasDefaultValue(false)
                .HasColumnName("is_banned");

            entity.Property(e => e.BannedAt)
                .HasColumnType("datetime")
                .HasColumnName("banned_at")
                .IsRequired(false);

            entity.Property(e => e.UnbannedAt)
                .HasColumnType("datetime")
                .HasColumnName("unbanned_at")
                .IsRequired(false);

            entity.Property(e => e.DeactivatedAt)
                .HasColumnType("datetime")
                .HasColumnName("deactivated_at")
                .IsRequired(false);
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.VoucherId).HasName("PK__Voucher__80B6FFA87E744E6D");

            entity.ToTable("Voucher");

            entity.HasIndex(e => e.VoucherCode, "UQ__Voucher__217310695E4DF069").IsUnique();

            entity.Property(e => e.VoucherId).HasColumnName("voucher_id");
            entity.Property(e => e.DiscountVal)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("discount_val");
            entity.Property(e => e.ValidFrom).HasColumnName("valid_from");
            entity.Property(e => e.ValidTo).HasColumnName("valid_to");
            entity.Property(e => e.VoucherCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("voucher_code");

            // Các trường mới
            entity.Property(e => e.ManagerId)
                .HasColumnName("manager_id")
                .HasDefaultValue(1);

            entity.Property(e => e.DiscountType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("discount_type")
                .HasDefaultValue("fixed");

            entity.Property(e => e.UsageLimit)
                .HasColumnName("usage_limit")
                .IsRequired(false);

            entity.Property(e => e.UsedCount)
                .HasColumnName("used_count")
                .HasDefaultValue(0);

            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description")
                .IsRequired(false);

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.IsDeleted)
                .HasColumnName("is_deleted")
                .HasDefaultValue(false);

            entity.Property(e => e.IsRestricted)
                .HasColumnName("is_restricted")
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired(false);

            entity.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at")
                .IsRequired(false);

            // Foreign key constraint
            entity.HasOne(d => d.Manager)
                .WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Voucher_Manager");
        });

        modelBuilder.Entity<VoucherEmailHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_VoucherEmailHistory");

            entity.ToTable("VoucherEmailHistory");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.VoucherId).HasColumnName("voucher_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.SentAt)
                .HasColumnName("sent_at")
                .HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status")
                .HasDefaultValue("success");

            // Foreign key constraints
            entity.HasOne(d => d.Voucher)
                .WithMany(p => p.VoucherEmailHistories)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VoucherEmailHistory_Voucher");

            entity.HasOne(d => d.User)
                .WithMany(p => p.VoucherEmailHistories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VoucherEmailHistory_User");
        });

        modelBuilder.Entity<UserVoucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_UserVoucher");

            entity.ToTable("UserVoucher");

            entity.HasIndex(e => new { e.VoucherId, e.UserId }, "IX_UserVoucher_Voucher_User")
                .IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.VoucherId).HasColumnName("voucher_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.IsUsed)
                .HasColumnName("is_used")
                .HasDefaultValue(false);
            entity.Property(e => e.UsedAt)
                .HasColumnName("used_at")
                .IsRequired(false);
            entity.Property(e => e.BookingId)
                .HasColumnName("booking_id")
                .IsRequired(false);
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // Foreign key constraints
            entity.HasOne(d => d.Voucher)
                .WithMany()
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserVoucher_Voucher");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserVoucher_User");

            entity.HasOne(d => d.Booking)
                .WithMany()
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_UserVoucher_Booking");
        });

        modelBuilder.Entity<VoucherReservation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_VoucherReservation");

            entity.ToTable("VoucherReservation");

            entity.HasIndex(e => e.VoucherId, "IX_VoucherReservation_Voucher_Active")
                .IsUnique()
                .HasFilter("[released_at] IS NULL");

            entity.HasIndex(e => e.SessionId, "IX_VoucherReservation_Session");

            entity.HasIndex(e => e.ExpiresAt, "IX_VoucherReservation_Expires")
                .HasFilter("[released_at] IS NULL");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.VoucherId).HasColumnName("voucher_id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ReservedAt)
                .HasColumnName("reserved_at")
                .HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.ReleasedAt)
                .HasColumnName("released_at")
                .IsRequired(false);

            // Foreign key constraints
            entity.HasOne(d => d.Voucher)
                .WithMany()
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_VoucherReservation_Voucher");

            entity.HasOne(d => d.Session)
                .WithMany()
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_VoucherReservation_BookingSession");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VoucherReservation_User");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Order__46596229");

            entity.ToTable("Order");

            entity.HasIndex(e => e.BookingSessionId, "IX_order_booking_session");

            entity.HasIndex(e => e.UserId, "IX_order_user")
                .HasFilter("([user_id] IS NOT NULL)");

            entity.HasIndex(e => e.Status, "IX_order_status");

            entity.HasIndex(e => e.ShowtimeId, "IX_order_showtime");

            entity.HasIndex(e => e.PaymentExpiresAt, "IX_order_expires")
                .HasFilter("([payment_expires_at] IS NOT NULL)");

            entity.HasIndex(e => e.PayOsOrderCode, "UX_order_payos_code")
                .IsUnique()
                .HasFilter("([payos_order_code] IS NOT NULL)");

            entity.HasIndex(e => e.BookingId, "IX_order_booking")
                .HasFilter("([booking_id] IS NOT NULL)");

            entity.Property(e => e.OrderId)
                .HasMaxLength(32)
                .HasColumnName("order_id");
            entity.Property(e => e.BookingSessionId).HasColumnName("booking_session_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ShowtimeId).HasColumnName("showtime_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("VND")
                .HasColumnName("currency");
            entity.Property(e => e.Provider)
                .HasMaxLength(32)
                .IsUnicode(false)
                .HasDefaultValue("payos")
                .HasColumnName("provider");
            entity.Property(e => e.Status)
                .HasMaxLength(24)
                .IsUnicode(false)
                .HasDefaultValue("PENDING")
                .HasColumnName("status");
            entity.Property(e => e.PayOsOrderCode)
                .HasMaxLength(128)
                .HasColumnName("payos_order_code");
            entity.Property(e => e.PayOsPaymentLink)
                .HasMaxLength(512)
                .HasColumnName("payos_payment_link");
            entity.Property(e => e.PayOsQrCode)
                .HasMaxLength(512)
                .HasColumnName("payos_qr_code");
            entity.Property(e => e.PaymentExpiresAt)
                .HasPrecision(3)
                .HasColumnName("payment_expires_at");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(100)
                .HasColumnName("customer_name");
            entity.Property(e => e.CustomerPhone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("customer_phone");
            entity.Property(e => e.CustomerEmail)
                .HasMaxLength(100)
                .HasColumnName("customer_email");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");

            entity.HasOne(d => d.BookingSession).WithMany()
                .HasForeignKey(d => d.BookingSessionId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Order_BookingSession");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Order_User");

            entity.HasOne(d => d.Showtime).WithMany()
                .HasForeignKey(d => d.ShowtimeId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Order_Showtime");

            entity.HasOne(d => d.Booking).WithMany()
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Order_Booking");
        });

        // VIP System Entities
        modelBuilder.Entity<VIPLevel>(entity =>
        {
            entity.HasKey(e => e.VipLevelId).HasName("PK_VIPLevel");

            entity.ToTable("VIPLevel");

            entity.Property(e => e.VipLevelId).HasColumnName("vip_level_id");
            entity.Property(e => e.LevelName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("level_name");
            entity.Property(e => e.LevelDisplayName)
                .HasMaxLength(100)
                .HasColumnName("level_display_name");
            entity.Property(e => e.MinPointsRequired).HasColumnName("min_points_required");
            entity.Property(e => e.PointEarningRate)
                .HasColumnType("decimal(5,2)")
                .HasColumnName("point_earning_rate");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<VIPBenefit>(entity =>
        {
            entity.HasKey(e => e.BenefitId).HasName("PK_VIPBenefit");

            entity.ToTable("VIPBenefit");

            entity.Property(e => e.BenefitId).HasColumnName("benefit_id");
            entity.Property(e => e.VipLevelId).HasColumnName("vip_level_id");
            entity.Property(e => e.BenefitType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("benefit_type");
            entity.Property(e => e.BenefitName)
                .HasMaxLength(200)
                .HasColumnName("benefit_name");
            entity.Property(e => e.BenefitDescription)
                .HasMaxLength(500)
                .HasColumnName("benefit_description");
            entity.Property(e => e.BenefitValue)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("benefit_value");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");

            entity.HasOne(d => d.VIPLevel).WithMany(p => p.VIPBenefits)
                .HasForeignKey(d => d.VipLevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VIPBenefit_VIPLevel");
        });

        modelBuilder.Entity<VIPMember>(entity =>
        {
            entity.HasKey(e => e.VipMemberId).HasName("PK_VIPMember");

            entity.ToTable("VIPMember");

            entity.HasIndex(e => e.CustomerId, "UQ_VIPMember_Customer").IsUnique();

            entity.Property(e => e.VipMemberId).HasColumnName("vip_member_id");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.CurrentVipLevelId).HasColumnName("current_vip_level_id");
            entity.Property(e => e.TotalPoints).HasColumnName("total_points");
            entity.Property(e => e.GrowthValue).HasColumnName("growth_value");
            entity.Property(e => e.LastUpgradeDate)
                .HasPrecision(3)
                .HasColumnName("last_upgrade_date");
            entity.Property(e => e.BirthdayBonusClaimedYear).HasColumnName("birthday_bonus_claimed_year");
            entity.Property(e => e.MonthlyFreeTicketClaimedMonth).HasColumnName("monthly_free_ticket_claimed_month");
            entity.Property(e => e.MonthlyFreeComboClaimedMonth).HasColumnName("monthly_free_combo_claimed_month");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(3)
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Customer).WithOne(p => p.VIPMember)
                .HasForeignKey<VIPMember>(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VIPMember_Customer");

            entity.HasOne(d => d.CurrentVIPLevel).WithMany(p => p.VIPMembers)
                .HasForeignKey(d => d.CurrentVipLevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VIPMember_VIPLevel");
        });

        modelBuilder.Entity<PointHistory>(entity =>
        {
            entity.HasKey(e => e.PointHistoryId).HasName("PK_PointHistory");

            entity.ToTable("PointHistory");

            entity.Property(e => e.PointHistoryId).HasColumnName("point_history_id");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.OrderId)
                .HasMaxLength(100)
                .HasColumnName("order_id");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("transaction_type");
            entity.Property(e => e.Points).HasColumnName("points");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.VipLevelId).HasColumnName("vip_level_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Customer).WithMany(p => p.PointHistories)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PointHistory_Customer");

            entity.HasOne(d => d.VIPLevel).WithMany(p => p.PointHistories)
                .HasForeignKey(d => d.VipLevelId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_PointHistory_VIPLevel");
        });

        modelBuilder.Entity<VIPBenefitClaim>(entity =>
        {
            entity.HasKey(e => e.BenefitClaimId).HasName("PK_VIPBenefitClaim");

            entity.ToTable("VIPBenefitClaim");

            entity.Property(e => e.BenefitClaimId).HasColumnName("benefit_claim_id");
            entity.Property(e => e.VipMemberId).HasColumnName("vip_member_id");
            entity.Property(e => e.BenefitId).HasColumnName("benefit_id");
            entity.Property(e => e.ClaimType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("claim_type");
            entity.Property(e => e.ClaimValue)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("claim_value");
            entity.Property(e => e.VoucherId).HasColumnName("voucher_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.ClaimedAt)
                .HasPrecision(3)
                .HasColumnName("claimed_at");
            entity.Property(e => e.ExpiresAt)
                .HasPrecision(3)
                .HasColumnName("expires_at");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");

            entity.HasOne(d => d.VIPMember).WithMany(p => p.VIPBenefitClaims)
                .HasForeignKey(d => d.VipMemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VIPBenefitClaim_VIPMember");

            entity.HasOne(d => d.VIPBenefit).WithMany(p => p.VIPBenefitClaims)
                .HasForeignKey(d => d.BenefitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VIPBenefitClaim_VIPBenefit");

            entity.HasOne(d => d.Voucher).WithMany()
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_VIPBenefitClaim_Voucher");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
