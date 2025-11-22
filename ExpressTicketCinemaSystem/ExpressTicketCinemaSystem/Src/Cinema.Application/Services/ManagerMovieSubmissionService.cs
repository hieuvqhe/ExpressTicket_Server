using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.MovieManagement.Responses;
using System.Text.RegularExpressions;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class ManagerMovieSubmissionService
    {
        private readonly CinemaDbCoreContext _context;

        // Các trạng thái hợp lệ cho màn manager (trừ Draft)
        private static readonly HashSet<string> NonDraftStatuses =
            new(StringComparer.OrdinalIgnoreCase) { "Pending", "Rejected", "Resubmitted", "Approved" };

        public ManagerMovieSubmissionService(CinemaDbCoreContext context)
        {
            _context = context;
        }

        // GET all (non-draft)
        public async Task<object> GetAllNonDraftSubmissionsAsync(
            int page, int limit, string? status, string? search, string? sortBy, string? sortOrder)
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            // EF Core không thể translate StringComparer.OrdinalIgnoreCase trong Contains,
            // nên dùng mảng lowercase để filter
            var nonDraftStatusesLower = new[] { "pending", "rejected", "resubmitted", "approved" };
            var q = _context.MovieSubmissions
                .Include(ms => ms.Partner)
                .Where(ms => nonDraftStatusesLower.Contains(ms.Status.ToLower()))
                .AsQueryable();

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
        public async Task<object> GetNonDraftSubmissionByIdAsync(int id)
        {
            var ms = await _context.MovieSubmissions
                .Include(x => x.MovieSubmissionActors).ThenInclude(a => a.Actor)
                .Include(x => x.Partner)
                .FirstOrDefaultAsync(x => x.MovieSubmissionId == id);

            if (ms == null)
                throw new NotFoundException("Không tìm thấy submission.");

            if (!NonDraftStatuses.Contains(ms.Status))
                throw new ValidationException("status", "Chỉ xem được submission đã gửi (không phải Draft).");

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
                Actors = actors
            };
        }

        // GET pending
        public async Task<object> GetPendingSubmissionsAsync(
            int page, int limit, string? search, string? sortBy, string? sortOrder)
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            var q = _context.MovieSubmissions
                .Include(ms => ms.Partner)
                .Where(ms => ms.Status == "Pending")
                .AsQueryable();

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
        public async Task<MovieSubmissionResponse> ApproveSubmissionAsync(int submissionId, int managerId)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            var s = await _context.MovieSubmissions
                .Include(x => x.MovieSubmissionActors)
                .FirstOrDefaultAsync(x => x.MovieSubmissionId == submissionId);

            if (s == null) throw new NotFoundException("Không tìm thấy submission.");
            if (s.Status != "Pending") throw new ValidationException("status", "Chỉ duyệt được submission ở trạng thái Pending.");
            if (s.ReviewerId != managerId) /* nếu muốn ràng đúng manager */
                throw new UnauthorizedException("Bạn không phải reviewer được phân công.");

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

            return await MapToMovieSubmissionResponseAsync(s);
        }
        public async Task<MovieSubmissionResponse> RejectSubmissionAsync(int submissionId, int managerId, string reason)
        {
            var s = await _context.MovieSubmissions.FirstOrDefaultAsync(x => x.MovieSubmissionId == submissionId);
            if (s == null) throw new NotFoundException("Không tìm thấy submission.");
            if (s.Status != "Pending") throw new ValidationException("status", "Chỉ từ chối được submission ở trạng thái Pending.");
            if (s.ReviewerId != managerId)
                throw new UnauthorizedException("Bạn không phải reviewer được phân công.");

            s.Status = "Rejected";
            s.ReviewerId = managerId;
            s.ReviewedAt = DateTime.UtcNow;
            s.RejectionReason = string.IsNullOrWhiteSpace(reason)
                ? "Hồ sơ chưa đạt yêu cầu."
                : reason.Trim();
            s.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
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

    }
}
