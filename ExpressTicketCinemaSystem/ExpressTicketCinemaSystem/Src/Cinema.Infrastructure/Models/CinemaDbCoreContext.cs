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

    public virtual DbSet<Cast> Casts { get; set; }

    public virtual DbSet<Cinema> Cinemas { get; set; }

    public virtual DbSet<Contract> Contracts { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<EmailChangeRequest> EmailChangeRequests { get; set; }

    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<GameShow> GameShows { get; set; }

    public virtual DbSet<Manager> Managers { get; set; }

    public virtual DbSet<Movie> Movies { get; set; }

    public virtual DbSet<MovieActor> MovieActors { get; set; }

    public virtual DbSet<MovieSubmission> MovieSubmissions { get; set; }

    public virtual DbSet<Partner> Partners { get; set; }

    public virtual DbSet<PartnerReport> PartnerReports { get; set; }

    public virtual DbSet<PasswordResetCode> PasswordResetCodes { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<RatingFilm> RatingFilms { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<RevenueReport> RevenueReports { get; set; }

    public virtual DbSet<Screen> Screens { get; set; }

    public virtual DbSet<Seat> Seats { get; set; }

    public virtual DbSet<SeatMap> SeatMaps { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ServiceOrder> ServiceOrders { get; set; }

    public virtual DbSet<Showtime> Showtimes { get; set; }

    public virtual DbSet<SupportTicket> SupportTickets { get; set; }

    public virtual DbSet<SystemAdmin> SystemAdmins { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

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
            entity.Property(e => e.RecordId).HasColumnName("record_id");
            entity.Property(e => e.TableName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("table_name");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("timestamp");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Booking__5DE3A5B1B58620C7");

            entity.ToTable("Booking");

            entity.HasIndex(e => e.BookingCode, "UQ__Booking__FF29040F62E5CCE1").IsUnique();

            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.BookingCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("booking_code");
            entity.Property(e => e.BookingTime)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("booking_time");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.ShowtimeId).HasColumnName("showtime_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_amount");
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

        modelBuilder.Entity<Cast>(entity =>
        {
            entity.HasKey(e => e.CastId).HasName("PK__Cast__D4C48F8809C4BE6C");

            entity.ToTable("Cast");

            entity.Property(e => e.CastId).HasColumnName("cast_id");
            entity.Property(e => e.Character)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("character");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("gender");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Cinema>(entity =>
        {
            entity.HasKey(e => e.CinemaId).HasName("PK__Cinema__56628778C5A53926");

            entity.ToTable("Cinema");

            entity.Property(e => e.CinemaId).HasColumnName("cinema_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("address");
            entity.Property(e => e.CinemaName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("cinema_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.PartnerId).HasColumnName("partner_id");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");

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
            entity.Property(e => e.SignedAt).HasColumnName("signed_at");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("draft")
                .HasColumnName("status");
            entity.Property(e => e.TermsAndConditions).HasColumnName("terms_and_conditions");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
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
                .IsUnicode(false)
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
                .IsUnicode(false)
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

        modelBuilder.Entity<GameShow>(entity =>
        {
            entity.HasKey(e => e.GameshowId).HasName("PK__GameShow__651C4D02FE1105CE");

            entity.ToTable("GameShow");

            entity.Property(e => e.GameshowId).HasColumnName("gameshow_id");
            entity.Property(e => e.Description)
                .HasColumnType("text")
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
                .IsUnicode(false)
                .HasColumnName("department");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .IsUnicode(false)
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
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("country");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(256)
                .HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Director)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("director");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.EndDate).HasColumnName("endDate");
            entity.Property(e => e.Genre)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("genre");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Language)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("language");
            entity.Property(e => e.PosterUrl).HasColumnName("poster_url");
            entity.Property(e => e.PremiereDate).HasColumnName("premiereDate");
            entity.Property(e => e.Production)
                .HasMaxLength(255)
                .HasColumnName("production");
            entity.Property(e => e.RatingsCount).HasColumnName("ratings_count");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("title");
            entity.Property(e => e.TrailerUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("trailer_url");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
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
            entity.HasKey(e => e.SubmissionId).HasName("PK__MovieSub__9B535595013C563F");

            entity.ToTable("MovieSubmission");

            entity.Property(e => e.SubmissionId).HasColumnName("submission_id");
            entity.Property(e => e.MovieId).HasColumnName("movie_id");
            entity.Property(e => e.PartnerId).HasColumnName("partner_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.SubmittedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("submitted_at");

            entity.HasOne(d => d.Movie).WithMany(p => p.MovieSubmissions)
                .HasForeignKey(d => d.MovieId)
                .HasConstraintName("FK_MovieSubmission_Movie");

            entity.HasOne(d => d.Partner).WithMany(p => p.MovieSubmissions)
                .HasForeignKey(d => d.PartnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovieSubmission_Partner");
        });

        modelBuilder.Entity<Partner>(entity =>
        {
            entity.HasKey(e => e.PartnerId).HasName("PK__Partner__576F1B2707E11F04");

            entity.ToTable("Partner");

            entity.HasIndex(e => e.UserId, "UQ__Partner__B9BE370EDBC635FC").IsUnique();

            entity.Property(e => e.PartnerId).HasColumnName("partner_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false)
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
                .IsUnicode(false)
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
                .IsUnicode(false)
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

            entity.HasIndex(e => e.BookingId, "UQ__Payment__5DE3A5B037C84034").IsUnique();

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.Method)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("method");
            entity.Property(e => e.PaidAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("paid_at");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

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
            entity.Property(e => e.CinemaId).HasColumnName("cinema_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.ScreenName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("screen_name");
            entity.Property(e => e.ScreenType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("screen_type");

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
            entity.Property(e => e.SeatNumber).HasColumnName("seat_number");
            entity.Property(e => e.SeatType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("seat_type");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Screen).WithMany(p => p.Seats)
                .HasForeignKey(d => d.ScreenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Seat_Screen");
        });

        modelBuilder.Entity<SeatMap>(entity =>
        {
            entity.HasKey(e => e.SeatMapId).HasName("PK__SeatMap__55CFDE03A2A6F0A8");

            entity.ToTable("SeatMap");

            entity.HasIndex(e => e.ScreenId, "UQ__SeatMap__CC19B67B52ED2058").IsUnique();

            entity.Property(e => e.SeatMapId).HasColumnName("seat_map_id");
            entity.Property(e => e.LayoutData).HasColumnName("layout_data");
            entity.Property(e => e.ScreenId).HasColumnName("screen_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Screen).WithOne(p => p.SeatMap)
                .HasForeignKey<SeatMap>(d => d.ScreenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SeatMap_Screen");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Service__3E0DB8AF2C0E5A4F");

            entity.ToTable("Service");

            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.CinemaId).HasColumnName("cinema_id");
            entity.Property(e => e.IsAvailable)
                .HasDefaultValue(true)
                .HasColumnName("is_available");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ServiceName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("service_name");

            entity.HasOne(d => d.Cinema).WithMany(p => p.Services)
                .HasForeignKey(d => d.CinemaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Service_Cinema");
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
            entity.Property(e => e.BasePrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("base_price");
            entity.Property(e => e.CinemaId).HasColumnName("cinema_id");
            entity.Property(e => e.MovieId).HasColumnName("movie_id");
            entity.Property(e => e.ScreenId).HasColumnName("screen_id");
            entity.Property(e => e.ShowDatetime).HasColumnName("show_datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

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
                .IsUnicode(false)
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
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
