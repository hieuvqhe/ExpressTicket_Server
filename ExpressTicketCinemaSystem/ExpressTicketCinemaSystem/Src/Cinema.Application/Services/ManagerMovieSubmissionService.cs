using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.MovieManagement.Responses;
using System.Text.RegularExpressions;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class ManagerMovieSubmissionService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IManagerStaffPermissionService _managerStaffPermissionService;

        // Các trạng thái hợp lệ cho màn manager (trừ Draft)
        private static readonly HashSet<string> NonDraftStatuses =
            new(StringComparer.OrdinalIgnoreCase) { "Pending", "Rejected", "Resubmitted", "Approved" };

        public ManagerMovieSubmissionService(
            CinemaDbCoreContext context, 
            IAuditLogService auditLogService,
            IManagerStaffPermissionService managerStaffPermissionService)
        {
            _context = context;
            _auditLogService = auditLogService;
            _managerStaffPermissionService = managerStaffPermissionService;
        }

        // GET all (non-draft)
        public async Task<object> GetAllNonDraftSubmissionsAsync(
            int page, int limit, string? status, string? search, string? sortBy, string? sortOrder, int? managerStaffId = null)
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            // EF Core không thể translate StringComparer.OrdinalIgnoreCase trong Contains,
            // nên dùng mảng lowercase để filter
            var nonDraftStatusesLower = new[] { "pending", "rejected", "resubmitted", "approved" };
            var q = _context.MovieSubmissions
                .Include(ms => ms.Partner)
                .Include(ms => ms.ManagerStaff)
                    .ThenInclude(ms => ms.User)
                .Where(ms => nonDraftStatusesLower.Contains(ms.Status.ToLower()))
                .AsQueryable();

            // If ManagerStaff, filter by partners they have MOVIE_SUBMISSION_READ permission
            if (managerStaffId.HasValue)
            {
                var assignedPartnerIds = await _context.Partners
                    .Where(p => p.ManagerStaffId == managerStaffId.Value)
                    .Select(p => p.PartnerId)
                    .ToListAsync();

                if (assignedPartnerIds.Count > 0)
                {
                    // Get partner IDs with MOVIE_SUBMISSION_READ permission
                    var partnerIdsWithPermission = await _context.ManagerStaffPartnerPermissions
                        .Where(msp => msp.ManagerStaffId == managerStaffId.Value
                            && (msp.PartnerId == null || assignedPartnerIds.Contains(msp.PartnerId.Value))
                            && msp.Permission.PermissionCode == "MOVIE_SUBMISSION_READ"
                            && msp.IsActive
                            && msp.Permission.IsActive)
                        .Select(msp => msp.PartnerId ?? 0)
                        .Distinct()
                        .ToListAsync();

                    // Check if has global permission (null)
                    var hasGlobalPermission = await _context.ManagerStaffPartnerPermissions
                        .AnyAsync(msp => msp.ManagerStaffId == managerStaffId.Value
                            && msp.PartnerId == null
                            && msp.Permission.PermissionCode == "MOVIE_SUBMISSION_READ"
                            && msp.IsActive
                            && msp.Permission.IsActive);

                    if (hasGlobalPermission)
                    {
                        // Global permission: show all submissions for assigned partners
                        q = q.Where(ms => assignedPartnerIds.Contains(ms.PartnerId));
                    }
                    else
                    {
                        // Specific permissions: only show submissions for partners with MOVIE_SUBMISSION_READ permission
                        var validPartnerIds = partnerIdsWithPermission.Where(p => p > 0).ToList();
                        if (validPartnerIds.Count > 0)
                        {
                            q = q.Where(ms => validPartnerIds.Contains(ms.PartnerId));
                        }
                        else
                        {
                            // No permission: return empty result
                            q = q.Where(ms => false);
                        }
                    }
                }
                else
                {
                    // No assigned partners: return empty result
                    q = q.Where(ms => false);
                }
            }

            // optional filter theo status
            if (!string.IsNullOrWhiteSpace(status) && NonDraftStatuses.Contains(status))
            {
                // EF Core không thể translate StringComparison.OrdinalIgnoreCase, nên dùng ToLower()
                var statusLower = status.Trim().ToLower();
                q = q.Where(ms => ms.Status.ToLower() == statusLower);
            }

            // optional search theo title/director
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(ms => ms.Title.ToLower().Contains(s) || ms.Director.ToLower().Contains(s));
            }

            // sort
            sortBy = (sortBy ?? "createdAt").ToLower();
            var asc = (sortOrder ?? "desc").Equals("asc", StringComparison.OrdinalIgnoreCase);

            q = sortBy switch
            {
                "title" => asc ? q.OrderBy(x => x.Title) : q.OrderByDescending(x => x.Title),
                "status" => asc ? q.OrderBy(x => x.Status) : q.OrderByDescending(x => x.Status),
                "submittedat" => asc ? q.OrderBy(x => x.SubmittedAt) : q.OrderByDescending(x => x.SubmittedAt),
                "updatedat" => asc ? q.OrderBy(x => x.UpdatedAt) : q.OrderByDescending(x => x.UpdatedAt),
                _ => asc ? q.OrderBy(x => x.CreatedAt) : q.OrderByDescending(x => x.CreatedAt),
            };

            var total = await q.CountAsync();

            var items = await q
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(ms => new
                {
                    ms.MovieSubmissionId,
                    ms.Title,
                    ms.Director,
                    ms.Genre,
                    ms.Status,
                    ms.SubmittedAt,
                    ms.CreatedAt,
                    ms.UpdatedAt,
                    Partner = ms.Partner != null ? new
                    {
                        ms.Partner.PartnerId,
                        ms.Partner.PartnerName
                    } : null,
                    ManagerStaff = ms.ManagerStaff != null ? new
                    {
                        ms.ManagerStaff.ManagerStaffId,
                        ms.ManagerStaff.FullName,
                        ms.ManagerStaff.User.Email
                    } : null
                })
                .ToListAsync();

            return new
            {
                submissions = items,
                pagination = new
                {
                    currentPage = page,
                    pageSize = limit,
                    totalCount = total,
                    totalPages = (int)Math.Ceiling(total / (double)limit)
                }
            };
        }

        // GET by id (non-draft only)
        public async Task<object> GetNonDraftSubmissionByIdAsync(int id, int? managerStaffId = null)
        {
            var ms = await _context.MovieSubmissions
                .Include(x => x.MovieSubmissionActors).ThenInclude(a => a.Actor)
                .Include(x => x.Partner)
                .Include(x => x.Reviewer).ThenInclude(r => r.User)
                .Include(x => x.ManagerStaff).ThenInclude(ms => ms.User)
                .FirstOrDefaultAsync(x => x.MovieSubmissionId == id);

            if (ms == null)
                throw new NotFoundException("Không tìm thấy submission.");

            if (!NonDraftStatuses.Contains(ms.Status))
                throw new ValidationException("status", "Chỉ xem được submission đã gửi (không phải Draft).");

            // If ManagerStaff, check permission
            if (managerStaffId.HasValue)
            {
                var hasPermission = await _managerStaffPermissionService.HasPermissionAsync(
                    managerStaffId.Value,
                    ms.PartnerId,
                    "MOVIE_SUBMISSION_READ");

                if (!hasPermission)
                {
                    throw new UnauthorizedException(new Dictionary<string, ValidationError>
                    {
                        ["permission"] = new ValidationError
                        {
                            Msg = "Bạn không có quyền xem submission này",
                            Path = "id",
                            Location = "path"
                        }
                    });
                }
            }

            var actors = ms.MovieSubmissionActors
                .Select(a => new
                {
                    a.MovieSubmissionActorId,
                    a.ActorId,
                    actorName = a.ActorId != null && a.Actor != null ? a.Actor.Name : a.ActorName,
                    actorAvatarUrl = a.ActorId != null && a.Actor != null ? a.Actor.AvatarUrl : a.ActorAvatarUrl,
                    role = a.Role
                })
                .ToList();

            return new
            {
                ms.MovieSubmissionId,
                ms.Title,
                ms.Director,
                ms.Genre,
                ms.DurationMinutes,
                ms.Description,
                ms.Language,
                ms.Country,
                ms.PosterUrl,
                ms.BannerUrl,
                ms.Production,
                ms.PremiereDate,
                ms.EndDate,
                ms.TrailerUrl,
                ms.CopyrightDocumentUrl,
                ms.DistributionLicenseUrl,
                ms.AdditionalNotes,
                ms.Status,
                ms.SubmittedAt,
                ms.ReviewedAt,
                ms.RejectionReason,
                ms.MovieId,
                ms.CreatedAt,
                ms.UpdatedAt,
                Partner = ms.Partner != null ? new
                {
                    ms.Partner.PartnerId,
                    ms.Partner.PartnerName
                } : null,
                Reviewer = ms.Reviewer != null ? new
                {
                    ms.Reviewer.ManagerId,
                    ms.Reviewer.User.Fullname,
                    ms.Reviewer.User.Email
                } : null,
                ManagerStaff = ms.ManagerStaff != null ? new
                {
                    ms.ManagerStaff.ManagerStaffId,
                    ms.ManagerStaff.FullName,
                    ms.ManagerStaff.User.Email
                } : null,
                Actors = actors
            };
        }

        // GET pending
        public async Task<object> GetPendingSubmissionsAsync(
            int page, int limit, string? search, string? sortBy, string? sortOrder, int? managerStaffId = null)
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            var q = _context.MovieSubmissions
                .Include(ms => ms.Partner)
                .Include(ms => ms.ManagerStaff)
                    .ThenInclude(ms => ms.User)
                .Where(ms => ms.Status == "Pending")
                .AsQueryable();

            // If ManagerStaff, filter by partners they have MOVIE_SUBMISSION_READ permission
            if (managerStaffId.HasValue)
            {
                var assignedPartnerIds = await _context.Partners
                    .Where(p => p.ManagerStaffId == managerStaffId.Value)
                    .Select(p => p.PartnerId)
                    .ToListAsync();

                if (assignedPartnerIds.Count > 0)
                {
                    // Get partner IDs with MOVIE_SUBMISSION_READ permission
                    var partnerIdsWithPermission = await _context.ManagerStaffPartnerPermissions
                        .Where(msp => msp.ManagerStaffId == managerStaffId.Value
                            && (msp.PartnerId == null || assignedPartnerIds.Contains(msp.PartnerId.Value))
                            && msp.Permission.PermissionCode == "MOVIE_SUBMISSION_READ"
                            && msp.IsActive
                            && msp.Permission.IsActive)
                        .Select(msp => msp.PartnerId ?? 0)
                        .Distinct()
                        .ToListAsync();

                    // Check if has global permission (null)
                    var hasGlobalPermission = await _context.ManagerStaffPartnerPermissions
                        .AnyAsync(msp => msp.ManagerStaffId == managerStaffId.Value
                            && msp.PartnerId == null
                            && msp.Permission.PermissionCode == "MOVIE_SUBMISSION_READ"
                            && msp.IsActive
                            && msp.Permission.IsActive);

                    if (hasGlobalPermission)
                    {
                        // Global permission: show all pending submissions for assigned partners
                        q = q.Where(ms => assignedPartnerIds.Contains(ms.PartnerId));
                    }
                    else
                    {
                        // Specific permissions: only show pending submissions for partners with MOVIE_SUBMISSION_READ permission
                        var validPartnerIds = partnerIdsWithPermission.Where(p => p > 0).ToList();
                        if (validPartnerIds.Count > 0)
                        {
                            q = q.Where(ms => validPartnerIds.Contains(ms.PartnerId));
                        }
                        else
                        {
                            // No permission: return empty result
                            q = q.Where(ms => false);
                        }
                    }
                }
                else
                {
                    // No assigned partners: return empty result
                    q = q.Where(ms => false);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(ms => ms.Title.ToLower().Contains(s) || ms.Director.ToLower().Contains(s));
            }

            sortBy = (sortBy ?? "submittedAt").ToLower();
            var asc = (sortOrder ?? "desc").Equals("asc", StringComparison.OrdinalIgnoreCase);

            q = sortBy switch
            {
                "title" => asc ? q.OrderBy(x => x.Title) : q.OrderByDescending(x => x.Title),
                "createdat" => asc ? q.OrderBy(x => x.CreatedAt) : q.OrderByDescending(x => x.CreatedAt),
                "updatedat" => asc ? q.OrderBy(x => x.UpdatedAt) : q.OrderByDescending(x => x.UpdatedAt),
                _ => asc ? q.OrderBy(x => x.SubmittedAt) : q.OrderByDescending(x => x.SubmittedAt),
            };

            var total = await q.CountAsync();

            var items = await q
                .Include(ms => ms.ManagerStaff)
                    .ThenInclude(ms => ms.User)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(ms => new
                {
                    ms.MovieSubmissionId,
                    ms.Title,
                    ms.Director,
                    ms.Status,
                    ms.SubmittedAt,
                    Partner = ms.Partner != null ? new
                    {
                        ms.Partner.PartnerId,
                        ms.Partner.PartnerName
                    } : null,
                    ManagerStaff = ms.ManagerStaff != null ? new
                    {
                        ms.ManagerStaff.ManagerStaffId,
                        ms.ManagerStaff.FullName,
                        ms.ManagerStaff.User.Email
                    } : null
                })
                .ToListAsync();

            return new
            {
                submissions = items,
                pagination = new
                {
                    currentPage = page,
                    pageSize = limit,
                    totalCount = total,
                    totalPages = (int)Math.Ceiling(total / (double)limit)
                }
            };
        }
        public async Task<MovieSubmissionResponse> ApproveSubmissionAsync(int submissionId, int managerId, int? managerStaffId = null)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            var s = await _context.MovieSubmissions
                .Include(x => x.MovieSubmissionActors)
                .Include(x => x.Partner)
                .FirstOrDefaultAsync(x => x.MovieSubmissionId == submissionId);

            if (s == null) throw new NotFoundException("Không tìm thấy submission.");
            if (s.Status != "Pending") throw new ValidationException("status", "Chỉ duyệt được submission ở trạng thái Pending.");

            // If ManagerStaff, check permission
            if (managerStaffId.HasValue)
            {
                var hasPermission = await _managerStaffPermissionService.HasPermissionAsync(
                    managerStaffId.Value,
                    s.PartnerId,
                    "MOVIE_SUBMISSION_APPROVE");

                if (!hasPermission)
                {
                    throw new UnauthorizedException(new Dictionary<string, ValidationError>
                    {
                        ["permission"] = new ValidationError
                        {
                            Msg = "Bạn không có quyền duyệt submission này",
                            Path = "id",
                            Location = "path"
                        }
                    });
                }
            }
            else
            {
                // Manager: check reviewer assignment (if exists)
                if (s.ReviewerId.HasValue && s.ReviewerId != managerId)
                    throw new UnauthorizedException("Bạn không phải reviewer được phân công.");
            }
            var beforeSnapshot = BuildSubmissionSnapshot(s);

            string key = NormalizeTitle(s.Title);

            // 1) Tạo Movie nếu chưa có (đặt unique bằng logic hoặc index ở Movie)
            // Lưu ý: NormalizeTitle là hàm .NET nên EF không translate được xuống SQL.
            // Vì vậy ta load danh sách title lên memory rồi so sánh.
            var existingMovieTitles = await _context.Movies
                .Select(m => m.Title)
                .ToListAsync();
            
            var movie = existingMovieTitles
                .FirstOrDefault(t => NormalizeTitle(t) == key);
            
            Movie? movieEntity = null;
            if (movie != null)
            {
                movieEntity = await _context.Movies.FirstOrDefaultAsync(m => m.Title == movie);
            }
            
            if (movieEntity == null)
            {
                // Lấy thông tin manager để set CreatedBy
                var manager = await _context.Managers
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.ManagerId == managerId);
                
                movieEntity = new Movie
                {
                    Title = s.Title.Trim(),
                    Genre = s.Genre,
                    DurationMinutes = s.DurationMinutes,
                    Director = s.Director,
                    Language = s.Language,
                    Country = s.Country,
                    IsActive = true, // Phim được duyệt thì active ngay
                    PosterUrl = s.PosterUrl,
                    BannerUrl = s.BannerUrl,
                    Production = s.Production,
                    Description = s.Description,
                    PremiereDate = s.PremiereDate,
                    EndDate = s.EndDate,
                    TrailerUrl = s.TrailerUrl,
                    PartnerId = s.PartnerId, // Link với partner đã submit
                    CreatedBy = manager?.User?.Fullname ?? $"Manager_{managerId}", // Tên manager hoặc fallback
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Movies.Add(movieEntity);
                await _context.SaveChangesAsync();
            }

            // 2) Đồng bộ actor: nếu có actor nháp (ActorId = null) → tạo Actor system rồi link MovieActors
            foreach (var msa in s.MovieSubmissionActors)
            {
                int actorId;
                if (msa.ActorId.HasValue)
                {
                    actorId = msa.ActorId.Value;
                }
                else
                {
                    var newActor = new Actor
                    {
                        Name = msa.ActorName ?? string.Empty,
                        AvatarUrl = msa.ActorAvatarUrl
                    };
                    _context.Actors.Add(newActor);
                    await _context.SaveChangesAsync();
                    actorId = newActor.ActorId;
                }
                _context.MovieActors.Add(new MovieActor
                {
                    MovieId = movieEntity.MovieId,
                    ActorId = actorId,
                    Role = msa.Role
                });
            }

            // 3) Cập nhật submission được duyệt
            s.Status = "Approved";
            s.MovieId = movieEntity.MovieId;
            s.ReviewerId = managerId;
            s.ManagerStaffId = managerStaffId; // Set ManagerStaffId if approved by ManagerStaff
            s.ReviewedAt = DateTime.UtcNow;
            s.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 4) Auto-reject các Pending khác trùng tiêu đề
            // Lưu ý: NormalizeTitle là hàm .NET nên EF không translate được xuống SQL.
            // Vì vậy ta load danh sách Pending lên memory rồi filter.
            var allPending = await _context.MovieSubmissions
                .Where(x => x.MovieSubmissionId != s.MovieSubmissionId
                        && x.Status == "Pending")
                .Select(x => new { x.MovieSubmissionId, x.Title })
                .ToListAsync();
            
            var others = allPending
                .Where(x => NormalizeTitle(x.Title) == key)
                .Select(x => x.MovieSubmissionId)
                .ToList();
            
            var othersEntities = await _context.MovieSubmissions
                .Where(x => others.Contains(x.MovieSubmissionId))
                .ToListAsync();

            foreach (var o in othersEntities)
            {
                o.Status = "Rejected";
                o.RejectionReason = "Đã có phim trùng";
                o.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

            await tx.CommitAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: "MANAGER_APPROVE_MOVIE_SUBMISSION",
                tableName: "MovieSubmission",
                recordId: s.MovieSubmissionId,
                beforeData: beforeSnapshot,
                afterData: BuildSubmissionSnapshot(s),
                metadata: new { managerId, s.MovieId });

            return await MapToMovieSubmissionResponseAsync(s);
        }
        public async Task<MovieSubmissionResponse> RejectSubmissionAsync(int submissionId, int managerId, string reason, int? managerStaffId = null)
        {
            var s = await _context.MovieSubmissions
                .Include(x => x.Partner)
                .FirstOrDefaultAsync(x => x.MovieSubmissionId == submissionId);
            if (s == null) throw new NotFoundException("Không tìm thấy submission.");
            if (s.Status != "Pending") throw new ValidationException("status", "Chỉ từ chối được submission ở trạng thái Pending.");

            // If ManagerStaff, check permission
            if (managerStaffId.HasValue)
            {
                var hasPermission = await _managerStaffPermissionService.HasPermissionAsync(
                    managerStaffId.Value,
                    s.PartnerId,
                    "MOVIE_SUBMISSION_REJECT");

                if (!hasPermission)
                {
                    throw new UnauthorizedException(new Dictionary<string, ValidationError>
                    {
                        ["permission"] = new ValidationError
                        {
                            Msg = "Bạn không có quyền từ chối submission này",
                            Path = "id",
                            Location = "path"
                        }
                    });
                }
            }
            else
            {
                // Manager: check reviewer assignment (if exists)
                if (s.ReviewerId.HasValue && s.ReviewerId != managerId)
                    throw new UnauthorizedException("Bạn không phải reviewer được phân công.");
            }
            var beforeSnapshot = BuildSubmissionSnapshot(s);

            s.Status = "Rejected";
            s.ReviewerId = managerId;
            s.ManagerStaffId = managerStaffId; // Set ManagerStaffId if rejected by ManagerStaff
            s.ReviewedAt = DateTime.UtcNow;
            s.RejectionReason = string.IsNullOrWhiteSpace(reason)
                ? "Hồ sơ chưa đạt yêu cầu."
                : reason.Trim();
            s.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogService.LogEntityChangeAsync(
                action: "MANAGER_REJECT_MOVIE_SUBMISSION",
                tableName: "MovieSubmission",
                recordId: s.MovieSubmissionId,
                beforeData: beforeSnapshot,
                afterData: BuildSubmissionSnapshot(s),
                metadata: new { managerId });
            return await MapToMovieSubmissionResponseAsync(s);
        }
        private static string NormalizeTitle(string? title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;

            // Lower -> bỏ ký tự không phải chữ/số -> thay nhiều khoảng trắng bằng một -> trim
            var t = title.ToLowerInvariant();
            t = Regex.Replace(t, @"[^a-z0-9\s]", "");   // bỏ dấu câu, ký tự đặc biệt
            t = Regex.Replace(t, @"\s+", " ");         // gom khoảng trắng
            return t.Trim();
        }

        // Map entity submission -> response (tối giản, đủ cho manager dùng)
        private async Task<MovieSubmissionResponse> MapToMovieSubmissionResponseAsync(MovieSubmission submission)
        {
            // Lấy actors của submission
            var actors = await _context.MovieSubmissionActors
                .Where(msa => msa.MovieSubmissionId == submission.MovieSubmissionId)
                .Select(msa => new SubmissionActorResponse
                {
                    MovieSubmissionActorId = msa.MovieSubmissionActorId,
                    ActorId = msa.ActorId,
                    ActorName = msa.ActorId != null && msa.Actor != null ? msa.Actor.Name : (msa.ActorName ?? string.Empty),
                    ActorAvatarUrl = msa.ActorId != null && msa.Actor != null ? msa.Actor.AvatarUrl : msa.ActorAvatarUrl,
                    Role = msa.Role,
                    IsExistingActor = msa.ActorId.HasValue
                })
                .AsNoTracking()
                .ToListAsync();

            return new MovieSubmissionResponse
            {
                MovieSubmissionId = submission.MovieSubmissionId,
                Title = submission.Title,
                Genre = submission.Genre,
                DurationMinutes = submission.DurationMinutes,
                Director = submission.Director,
                Language = submission.Language,
                Country = submission.Country,
                PosterUrl = submission.PosterUrl,
                BannerUrl = submission.BannerUrl,
                Production = submission.Production,
                Description = submission.Description,
                PremiereDate = submission.PremiereDate,
                EndDate = submission.EndDate,
                TrailerUrl = submission.TrailerUrl,
                CopyrightDocumentUrl = submission.CopyrightDocumentUrl,
                DistributionLicenseUrl = submission.DistributionLicenseUrl,
                AdditionalNotes = submission.AdditionalNotes,
                Status = submission.Status,
                SubmittedAt = submission.SubmittedAt,
                ReviewedAt = submission.ReviewedAt,
                RejectionReason = submission.RejectionReason,
                MovieId = submission.MovieId,
                CreatedAt = submission.CreatedAt,
                UpdatedAt = submission.UpdatedAt,
                Actors = actors
            };
        }

        private static object BuildSubmissionSnapshot(MovieSubmission submission) => new
        {
            submission.MovieSubmissionId,
            submission.PartnerId,
            submission.Title,
            submission.Genre,
            submission.DurationMinutes,
            submission.Director,
            submission.Language,
            submission.Country,
            submission.PosterUrl,
            submission.Status,
            submission.SubmittedAt,
            submission.ReviewedAt,
            submission.RejectionReason,
            submission.MovieId,
            submission.CreatedAt,
            submission.UpdatedAt
        };

    }
}
