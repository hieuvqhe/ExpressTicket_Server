using Microsoft.EntityFrameworkCore;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;
using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.MovieManagement.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.MovieManagement.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Movie.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Enum;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services

{
    public class PartnerMovieManagementService
    {
        private readonly CinemaDbCoreContext _context;
        private readonly ILogger<PartnerMovieManagementService> _logger;

        public PartnerMovieManagementService(
       CinemaDbCoreContext context,
       ILogger<PartnerMovieManagementService> logger)
        {
            _context = context;
            _logger = logger;
        }
        private void LogStage(string stage, object? payload = null)
        {
            if (payload == null)
                _logger.LogInformation("[CreateSubmission] {Stage}", stage);
            else
                _logger.LogInformation("[CreateSubmission] {Stage} | payload={Payload}", stage, JsonSerializer.Serialize(payload));
        }

        private void LogError(string stage, Exception ex, object? payload = null)
        {
            _logger.LogError(ex, "[CreateSubmission][ERR] {Stage} | payload={Payload}", stage,
                payload == null ? "-" : JsonSerializer.Serialize(payload));
        }

        public async Task<Partner> GetPartnerByUserIdAsync(int userId)
        {
            var partner = await _context.Partners
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Status == "approved");

            if (partner == null)
                throw new UnauthorizedException("Partner không tồn tại hoặc chưa được duyệt");

            if (!partner.IsActive)
                throw new UnauthorizedException("Tài khoản partner đã bị vô hiệu hóa");

            return partner;
        }

        // ==================== AVAILABLE ACTORS METHODS ====================

        public async Task<PaginatedAvailableActorsResponse> GetAvailableActorsAsync(
    int page = 1,
    int limit = 10,
    string? search = null,
    string? sortBy = "name",
    string? sortOrder = "asc")
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            var query = _context.Actors.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim().ToLower()}%";
                query = query.Where(a => a.Name != null && EF.Functions.Like(a.Name.ToLower(), pattern));
            }

            var totalCount = await query.CountAsync();
            query = ApplyActorSorting(query, sortBy, sortOrder);

            var actors = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(a => new AvailableActorResponse
                {
                    Id = a.ActorId,
                    Name = a.Name,
                    ProfileImage = a.AvatarUrl
                })
                .AsNoTracking()
                .ToListAsync();

            var pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = limit,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)limit)
            };

            return new PaginatedAvailableActorsResponse
            {
                Actors = actors,
                Pagination = pagination
            };
        }

        public async Task<AvailableActorResponse> GetAvailableActorByIdAsync(int actorId)
        {
            var actor = await _context.Actors
                .FirstOrDefaultAsync(a => a.ActorId == actorId);

            if (actor == null)
                throw new NotFoundException("Không tìm thấy diễn viên với ID này.");

            return new AvailableActorResponse
            {
                Id = actor.ActorId,
                Name = actor.Name,
                ProfileImage = actor.AvatarUrl
            };
        }

        // ==================== SUBMISSION ACTORS METHODS ====================

        public async Task<SubmissionActorResponse> AddActorToSubmissionAsync(
    int submissionId, int partnerId, AddActorToSubmissionRequest request)
        {
            string Tag(string step) => $"[AddActor submissionId={submissionId}] {step}";

            // Helper chuẩn hóa tên so sánh
            string N(string? s) => (s ?? string.Empty).Trim().ToLowerInvariant();

            try
            {
                Console.WriteLine(Tag($"START | payload: actorId={request.ActorId}, actorName='{request.ActorName}', role='{request.Role}', avatar='{request.ActorAvatarUrl}'"));

                // 0) Quyền truy cập + trạng thái draft
                var submission = await ValidateSubmissionAccessAsync(submissionId, partnerId);
                Console.WriteLine(Tag($"Validated submission access | status={submission.Status}"));

                // 1) Validate role
                if (string.IsNullOrWhiteSpace(request.Role))
                    throw new ValidationException("role", "Vai diễn là bắt buộc");

                // (Tùy chọn) validate URL
                if (!string.IsNullOrWhiteSpace(request.ActorAvatarUrl) &&
                    !Uri.TryCreate(request.ActorAvatarUrl, UriKind.Absolute, out _))
                    throw new ValidationException("actorAvatarUrl", "URL avatar không hợp lệ");

                // 2) TH1: Chọn theo ActorId (link actor HỆ THỐNG)
                if (request.ActorId.HasValue)
                {
                    Console.WriteLine(Tag($"Link by ActorId={request.ActorId.Value}"));

                    var actor = await _context.Actors
                        .FirstOrDefaultAsync(a => a.ActorId == request.ActorId.Value);

                    if (actor == null)
                        throw new NotFoundException("Không tìm thấy diễn viên trong hệ thống");

                    // Tránh trùng: cùng ActorId hoặc đã có NHÁP trùng tên với actor hệ thống
                    var nSys = N(actor.Name);

                    var dup = await _context.MovieSubmissionActors
    .Where(msa => msa.MovieSubmissionId == submissionId)
    .AnyAsync(msa =>
        (msa.ActorId.HasValue && msa.ActorId == actor.ActorId) ||
        (!msa.ActorId.HasValue &&
         msa.ActorName != null &&
         msa.ActorName.Trim().ToLower() == nSys)
    );

                    if (dup)
                    {
                        Console.WriteLine(Tag($"CONFLICT | actor already exists in submission (by id or name)"));
                        throw new ConflictException("actorId", "Diễn viên đã có trong submission này");
                    }

                    var link = new MovieSubmissionActor
                    {
                        MovieSubmissionId = submissionId,
                        ActorId = actor.ActorId,
                        ActorName = actor.Name,            // lưu lại để hiển thị nhanh
                        ActorAvatarUrl = actor.AvatarUrl,  // lưu lại để hiển thị nhanh
                        Role = request.Role.Trim()
                    };

                    _context.MovieSubmissionActors.Add(link);

                    try
                    {
                        await _context.SaveChangesAsync();
                        Console.WriteLine(Tag($"SUCCESS | linked system actor id={actor.ActorId}"));
                    }
                    catch (DbUpdateException ex)
                    {
                        var msg = ex.InnerException?.Message ?? ex.Message;
                        Console.WriteLine(Tag($"DBERROR | {msg}"));
                        throw new ValidationException("database", $"Lỗi CSDL khi thêm diễn viên (link by id): {msg}", "database");
                    }

                    return MapToSubmissionActorResponse(link);
                }

                // 3) TH2: Không có ActorId -> theo ActorName
                if (string.IsNullOrWhiteSpace(request.ActorName))
                    throw new ValidationException("actorName", "Tên diễn viên là bắt buộc khi không chọn ActorId");

                var nReq = N(request.ActorName);
                Console.WriteLine(Tag($"Add by ActorName='{request.ActorName}' (n='{nReq}')"));

                // 3.1) Nếu tên đã tồn tại trong hệ thống -> tự động LINK hệ thống
                var existingSystem = await _context.Actors
    .OrderBy(a => a.ActorId)
    .FirstOrDefaultAsync(a => a.Name.Trim().ToLower() == nReq);

                if (existingSystem != null)
                {
                    Console.WriteLine(Tag($"Found system actor id={existingSystem.ActorId} name='{existingSystem.Name}' -> link"));

                    // Tránh trùng trong submission
                    var dupLink = await _context.MovieSubmissionActors
       .Where(msa => msa.MovieSubmissionId == submissionId)
       .AnyAsync(msa =>
           (msa.ActorId.HasValue && msa.ActorId == existingSystem.ActorId) ||
           (!msa.ActorId.HasValue &&
            msa.ActorName != null &&
            msa.ActorName.Trim().ToLower() == nReq)
       );

                    if (dupLink)
                    {
                        Console.WriteLine(Tag($"CONFLICT | actor already exists in submission (link by name to system)"));
                        throw new ConflictException("actorName", "Diễn viên đã có trong submission này");
                    }

                    var link = new MovieSubmissionActor
                    {
                        MovieSubmissionId = submissionId,
                        ActorId = existingSystem.ActorId,
                        ActorName = existingSystem.Name,
                        ActorAvatarUrl = existingSystem.AvatarUrl,
                        Role = request.Role.Trim()
                    };

                    _context.MovieSubmissionActors.Add(link);

                    try
                    {
                        await _context.SaveChangesAsync();
                        Console.WriteLine(Tag($"SUCCESS | linked to system actor id={existingSystem.ActorId}"));
                    }
                    catch (DbUpdateException ex)
                    {
                        var msg = ex.InnerException?.Message ?? ex.Message;
                        Console.WriteLine(Tag($"DBERROR | {msg}"));
                        throw new ValidationException("database", $"Lỗi CSDL khi thêm diễn viên (link by name): {msg}", "database");
                    }

                    return MapToSubmissionActorResponse(link);
                }

                // 3.2) Chưa có trong hệ thống -> tạo actor NHÁP (ActorId = null)
                // Tránh trùng NHÁP trong chính submission
                var dupDraft = await _context.MovieSubmissionActors
    .Where(msa => msa.MovieSubmissionId == submissionId && !msa.ActorId.HasValue)
    .AnyAsync(msa =>
        msa.ActorName != null &&
        msa.ActorName.Trim().ToLower() == nReq
    );

                if (dupDraft)
                {
                    Console.WriteLine(Tag($"CONFLICT | draft actor name already exists in submission"));
                    throw new ConflictException("actorName", "Diễn viên đã có trong submission này");
                }

                var draft = new MovieSubmissionActor
                {
                    MovieSubmissionId = submissionId,
                    ActorId = null,                         // NHÁP
                    ActorName = request.ActorName.Trim(),
                    ActorAvatarUrl = request.ActorAvatarUrl,
                    Role = request.Role.Trim()
                };

                _context.MovieSubmissionActors.Add(draft);

                try
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine(Tag($"SUCCESS | created DRAFT actor row id={draft.MovieSubmissionActorId} (actor_id=NULL)"));
                }
                catch (DbUpdateException ex)
                {
                    // Đây là chỗ hay báo 500 nếu DB không cho NULL ở actor_id hoặc tràn độ dài cột
                    var msg = ex.InnerException?.Message ?? ex.Message;
                    Console.WriteLine(Tag($"DBERROR | {msg}"));
                    throw new ValidationException("database", $"Lỗi CSDL khi thêm diễn viên (draft): {msg}", "database");
                }

                return MapToSubmissionActorResponse(draft);
            }
            catch (ValidationException) { throw; }   // đã có message rõ
            catch (ConflictException) { throw; }   // đã có message rõ
            catch (NotFoundException) { throw; }   // đã có message rõ
            catch (Exception ex)
            {
                // Chốt: log bất ngờ khác
                Console.WriteLine(Tag($"UNEXPECTED ERROR | {ex.Message}\n{ex.StackTrace}"));
                throw; // để controller trả 500 cho đúng, nhưng log đã đủ để tìm gốc
            }
        }

        public async Task<SubmissionActorsListResponse> GetSubmissionActorsAsync(int submissionId, int partnerId)
        {
            await ValidateSubmissionAccessAsync(submissionId, partnerId);

            var actors = await _context.MovieSubmissionActors
                .Where(msa => msa.MovieSubmissionId == submissionId)
                .Include(msa => msa.Actor)
                .ToListAsync();

            var actorResponses = actors.Select(MapToSubmissionActorResponse).ToList();

            return new SubmissionActorsListResponse
            {
                Actors = actorResponses,
                TotalCount = actorResponses.Count
            };
        }

        public async Task<SubmissionActorResponse> UpdateSubmissionActorAsync(
     int submissionId, int submissionActorId, int partnerId, UpdateSubmissionActorRequest request)
        {
            Console.WriteLine($"[UpdateActor] submissionId={submissionId}, submissionActorId={submissionActorId}");
            try
            {
                var submission = await ValidateSubmissionAccessAsync(submissionId, partnerId);

                var submissionActor = await _context.MovieSubmissionActors
                    .Include(msa => msa.Actor)
                    .FirstOrDefaultAsync(msa => msa.MovieSubmissionActorId == submissionActorId
                                             && msa.MovieSubmissionId == submissionId);
                if (submissionActor == null)
                    throw new NotFoundException("Không tìm thấy diễn viên trong submission");

                if (string.IsNullOrWhiteSpace(request.Role))
                    throw new ValidationException("role", "Vai diễn là bắt buộc");

                // === LINK ACTOR HỆ THỐNG: chỉ cho phép đổi role ===
                if (submissionActor.ActorId.HasValue)
                {
                    if (!string.IsNullOrWhiteSpace(request.ActorName) ||
                        !string.IsNullOrWhiteSpace(request.ActorAvatarUrl))
                    {
                        throw new ValidationException("actor",
                            "Không thể đổi thông tin diễn viên hệ thống, chỉ được đổi vai diễn");
                    }

                    submissionActor.Role = request.Role.Trim();
                    await _context.SaveChangesAsync();
                    Console.WriteLine("[UpdateActor] updated role for system-linked actor");
                    return MapToSubmissionActorResponse(submissionActor);
                }

                // === ACTOR NHÁP ===
                if (!string.IsNullOrWhiteSpace(request.ActorAvatarUrl) &&
                    !Uri.TryCreate(request.ActorAvatarUrl, UriKind.Absolute, out _))
                {
                    throw new ValidationException("actorAvatarUrl", "URL avatar không hợp lệ");
                }

                // Nếu đổi tên
                if (!string.IsNullOrWhiteSpace(request.ActorName))
                {
                    var nReq = request.ActorName.Trim().ToLower();

                    // Tìm trong hệ thống (KHÔNG dùng local function trong expression)
                    var systemActor = await _context.Actors
                        .OrderBy(a => a.ActorId)
                        .FirstOrDefaultAsync(a =>
                            a.Name != null && a.Name.Trim().ToLower() == nReq);

                    if (systemActor != null)
                    {
                        // Nếu submission đã link actor này rồi -> trùng
                        var dupLink = await _context.MovieSubmissionActors
                            .AnyAsync(msa => msa.MovieSubmissionId == submissionId &&
                                             msa.ActorId == systemActor.ActorId);
                        if (dupLink)
                            throw new ConflictException("actorName", "Diễn viên đã có trong submission này");

                        // Auto-link sang actor hệ thống
                        submissionActor.ActorId = systemActor.ActorId;
                        submissionActor.ActorName = systemActor.Name;
                        submissionActor.ActorAvatarUrl = systemActor.AvatarUrl;
                        Console.WriteLine($"[UpdateActor] auto-linked to system actor #{systemActor.ActorId} ({systemActor.Name})");
                    }
                    else
                    {
                        // Đổi tên actor nháp
                        submissionActor.ActorName = request.ActorName.Trim();

                        // Check trùng actor nháp khác trong submission
                        var dupDraft = await _context.MovieSubmissionActors
                            .AnyAsync(msa => msa.MovieSubmissionId == submissionId &&
                                             !msa.ActorId.HasValue &&
                                             msa.MovieSubmissionActorId != submissionActorId &&
                                             msa.ActorName != null &&
                                             msa.ActorName.Trim().ToLower() == nReq);
                        if (dupDraft)
                            throw new ConflictException("actorName", "Diễn viên đã có trong submission này");
                    }
                }

                if (request.ActorAvatarUrl != null)
                    submissionActor.ActorAvatarUrl = request.ActorAvatarUrl;

                submissionActor.Role = request.Role.Trim();

                await _context.SaveChangesAsync();
                Console.WriteLine("[UpdateActor] draft actor updated OK");
                return MapToSubmissionActorResponse(submissionActor);
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine("[UpdateActor][DB] " + dbEx.Message);
                Console.WriteLine("[UpdateActor][DB-Inner] " + dbEx.InnerException?.Message);
                throw; // controller sẽ trả 500, hoặc bạn có thể wrap thành ErrorResponse cụ thể
            }
            catch (Exception ex)
            {
                Console.WriteLine("[UpdateActor][EX] " + ex.GetType().Name + " - " + ex.Message);
                throw;
            }
        }

        public async Task RemoveActorFromSubmissionAsync(int submissionId, int submissionActorId, int partnerId)
        {
            var submission = await ValidateSubmissionAccessAsync(submissionId, partnerId);

            var submissionActor = await _context.MovieSubmissionActors
                .FirstOrDefaultAsync(msa => msa.MovieSubmissionActorId == submissionActorId
                                         && msa.MovieSubmissionId == submissionId);

            if (submissionActor == null)
                throw new NotFoundException("Không tìm thấy diễn viên trong submission");

            _context.MovieSubmissionActors.Remove(submissionActor);
            await _context.SaveChangesAsync();
        }

        // ==================== MOVIE SUBMISSION CRUD METHODS ====================

        public async Task<MovieSubmissionResponse> CreateMovieSubmissionAsync(int partnerId, CreateMovieSubmissionRequest request)
        {
            // Kiểm tra request null để tránh NullReferenceException
            if (request == null)
            {
                _logger.LogError("[CreateSubmission] Request is null");
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["request"] = new ValidationError
                    {
                        Msg = "Request không được để trống",
                        Path = "body",
                        Location = "body"
                    }
                });
            }

            var sw = Stopwatch.StartNew();
            using var scope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["partnerId"] = partnerId,
                ["traceId"] = Guid.NewGuid().ToString("N")
            });

            LogStage("START", new { partnerId, title = request?.Title ?? "null" });

            // Mọi thao tác bọc trong transaction để không “nửa vời”
            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1) Validate
                LogStage("VALIDATE:BEGIN");
                await ValidateMovieSubmissionRequestAsync(request);
                LogStage("VALIDATE:DONE");

                // 2) Kiểm tra trùng nháp
                LogStage("CHECK_DUPLICATE_DRAFT:BEGIN");
                var duplicate = await _context.MovieSubmissions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ms => ms.PartnerId == partnerId
                        && ms.Title.ToLower() == request.Title.Trim().ToLower()
                        && ms.Status == SubmissionStatus.Draft.ToString());

                if (duplicate != null)
                {
                    LogStage("CHECK_DUPLICATE_DRAFT:HIT", new { duplicateId = duplicate.MovieSubmissionId });
                    throw new ConflictException("title",
                        $"Đã tồn tại bản nháp với tiêu đề '{request.Title}'. Vui lòng sử dụng bản nháp hiện có hoặc đổi tiêu đề.");
                }
                LogStage("CHECK_DUPLICATE_DRAFT:MISS");
                // 2.1) Kiểm tra phim đã tồn tại trong bảng Movies (so sánh theo tiêu đề, không phân biệt hoa/thường, bỏ khoảng trắng đầu/cuối)
                LogStage("CHECK_EXISTING_MOVIE_BY_TITLE:BEGIN");

                var titleKey = request.Title.Trim().ToLower();

                var movieTitleExisted = await _context.Movies
                    .AsNoTracking()
                    .AnyAsync(m => m.Title != null && m.Title.Trim().ToLower() == titleKey);

                if (movieTitleExisted)
                {
                    LogStage("CHECK_EXISTING_MOVIE_BY_TITLE:HIT", new { normalizedTitle = titleKey });
                    throw new ConflictException("title",
                        "Phim này đã tồn tại trên hệ thống . Hãy kiểm tra lại và lấy phim trên hệ thống để tạo lịch chiếu của bạn . Trân trọng");
                }

                LogStage("CHECK_EXISTING_MOVIE_BY_TITLE:MISS");
                // 3) Tạo submission
                LogStage("CREATE_SUBMISSION:BEGIN");
                var submission = new MovieSubmission
                {
                    PartnerId = partnerId,
                    Title = request.Title.Trim(),
                    Genre = request.Genre.Trim(),
                    DurationMinutes = request.DurationMinutes,
                    Director = request.Director.Trim(),
                    Language = request.Language.Trim(),
                    Country = request.Country.Trim(),
                    PosterUrl = request.PosterUrl,
                    BannerUrl = request.BannerUrl,
                    Production = request.Production?.Trim() ?? "",
                    Description = request.Description?.Trim() ?? "",
                    PremiereDate = request.PremiereDate,
                    EndDate = request.EndDate,
                    TrailerUrl = request.TrailerUrl,
                    CopyrightDocumentUrl = request.CopyrightDocumentUrl,
                    DistributionLicenseUrl = request.DistributionLicenseUrl,
                    AdditionalNotes = request.AdditionalNotes?.Trim(),
                    Status = SubmissionStatus.Draft.ToString(),
                    SubmittedAt = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.MovieSubmissions.Add(submission);
                await _context.SaveChangesAsync();
                LogStage("CREATE_SUBMISSION:DONE", new { submissionId = submission.MovieSubmissionId });

                // 4) Thêm diễn viên (nếu có)
                if ((request.ActorIds?.Any() ?? false) || (request.NewActors?.Any() ?? false))
                {
                    LogStage("ADD_ACTORS:BEGIN", new
                    {
                        actorIds = request.ActorIds,
                        newActors = request.NewActors?.Select(x => new { x.Name, x.AvatarUrl, x.Role }),
                        actorRoles = request.ActorRoles
                    });

                    await AddActorsToSubmission_BulkAutoLinkAsync(submission.MovieSubmissionId, request);
                    await _context.SaveChangesAsync();

                    LogStage("ADD_ACTORS:DONE");
                }
                else
                {
                    LogStage("ADD_ACTORS:SKIP");
                }

                // 5) Build response
                LogStage("BUILD_RESPONSE:BEGIN");
                var result = await GetMovieSubmissionResponseAsync(submission.MovieSubmissionId);
                LogStage("BUILD_RESPONSE:DONE");

                // 6) Commit
                await tx.CommitAsync();
                sw.Stop();
                LogStage("SUCCESS", new { elapsedMs = sw.ElapsedMilliseconds, submissionId = result.MovieSubmissionId });

                return result;
            }
            catch (ValidationException vex)
            {
                // ValidationException của bạn có thể chứa Dictionary<string, ValidationError>
                LogError("VALIDATION_FAIL", vex, vex.Errors);
                await tx.RollbackAsync();
                throw; // để controller trả mã 400 kèm lỗi cụ thể
            }
            catch (ConflictException cex)
            {
                LogError("CONFLICT", cex);
                await tx.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                LogError("UNEXPECTED", ex, new
                {
                    request = new
                    {
                        request.Title,
                        request.Director,
                        request.PremiereDate,
                        request.EndDate,
                        hasActors = (request.ActorIds?.Any() ?? false) || (request.NewActors?.Any() ?? false)
                    }
                });

                try { await tx.RollbackAsync(); } catch (Exception rex) { _logger.LogError(rex, "[CreateSubmission][ERR] rollback failed"); }

                // có thể bọc lại thành custom ApplicationException nếu muốn thông điệp chuẩn
                throw;
            }
            finally
            {
                LogStage("END", new { elapsedMs = sw.ElapsedMilliseconds });
            }
        }


        public async Task<PaginatedMovieSubmissionsResponse> GetMovieSubmissionsAsync(
            int partnerId,
            int page = 1,
            int limit = 10,
            string? status = null,
            string? search = null,
            string? sortBy = "createdAt",
            string? sortOrder = "desc")
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            var query = _context.MovieSubmissions
                .Where(ms => ms.PartnerId == partnerId)
                .Include(ms => ms.MovieSubmissionActors)
                    .ThenInclude(msa => msa.Actor)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SubmissionStatus>(status, true, out var statusEnum))
            {
                query = query.Where(ms => ms.Status == statusEnum.ToString());
            }

            // Search by title
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(ms => ms.Title.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync();
            query = ApplySubmissionSorting(query, sortBy, sortOrder);

            var submissions = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var submissionResponses = new List<MovieSubmissionResponse>();
            foreach (var submission in submissions)
            {
                submissionResponses.Add(await MapToMovieSubmissionResponseAsync(submission));
            }

            var pagination = new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = limit,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)limit)
            };

            return new PaginatedMovieSubmissionsResponse
            {
                Submissions = submissionResponses,
                Pagination = pagination
            };
        }

        public async Task<MovieSubmissionResponse> GetMovieSubmissionByIdAsync(int submissionId, int partnerId)
        {
            var submission = await _context.MovieSubmissions
                .Include(ms => ms.MovieSubmissionActors)
                    .ThenInclude(msa => msa.Actor)
                .FirstOrDefaultAsync(ms => ms.MovieSubmissionId == submissionId && ms.PartnerId == partnerId);

            if (submission == null)
                throw new NotFoundException("Không tìm thấy bản nháp phim");

            return await MapToMovieSubmissionResponseAsync(submission);
        }
        private static string N(string? s) => (s ?? "").Trim().ToLowerInvariant();

        private async Task AddActorsToSubmission_BulkAutoLinkAsync(int submissionId, CreateMovieSubmissionRequest request)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("[AddActorsBulk] START | submissionId={SubmissionId}", submissionId);
            var toAdd = new List<MovieSubmissionActor>();

            // 1) Link các actor hệ thống theo ID
            if (request.ActorIds?.Any() == true)
            {
                var ids = request.ActorIds.Distinct().ToList();

                // loại các ID đã có sẵn trong submission
                var alreadyLinkedIds = await _context.MovieSubmissionActors
                    .Where(msa => msa.MovieSubmissionId == submissionId && msa.ActorId != null)
                    .Select(msa => msa.ActorId!.Value)
                    .ToListAsync();

                var linkIds = ids.Except(alreadyLinkedIds).ToList();
                if (linkIds.Any())
                {
                    var actors = await _context.Actors
                        .Where(a => linkIds.Contains(a.ActorId))
                        .ToListAsync();

                    foreach (var a in actors)
                    {
                        var role = (request.ActorRoles != null && request.ActorRoles.TryGetValue(a.ActorId, out var r) && !string.IsNullOrWhiteSpace(r))
                            ? r.Trim() : "Diễn viên";

                        toAdd.Add(new MovieSubmissionActor
                        {
                            MovieSubmissionId = submissionId,
                            ActorId = a.ActorId,
                            ActorName = a.Name,
                            ActorAvatarUrl = a.AvatarUrl,
                            Role = role
                        });
                    }
                }
            }

            // 2) Xử lý NewActors: auto-link nếu trùng tên hệ thống, ngược lại tạo draft
            if (request.NewActors?.Any() == true)
            {
                // Lấy danh sách tên đã có trong submission (kể cả link & draft) để chống trùng
                var existingNamesInSubmission = await _context.MovieSubmissionActors
                    .Where(msa => msa.MovieSubmissionId == submissionId)
                    .Select(msa => msa.ActorId != null ? msa.Actor!.Name : msa.ActorName)
                    .ToListAsync();
                var nameSet = existingNamesInSubmission.Select(N).ToHashSet();

                foreach (var na in request.NewActors)
                {
                    var desiredName = (na.Name ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(desiredName)) continue; // bỏ qua rỗng

                    var desiredKey = desiredName.ToLower();
                    if (nameSet.Contains(desiredKey))
                    {
                        // đã có trong submission -> bỏ qua (không thêm trùng)
                        continue;
                    }

                    // Thử tìm trong hệ thống theo tên (so sánh thường, trim+lower)
                    var sys = await _context.Actors
    .OrderBy(a => a.ActorId)
    .FirstOrDefaultAsync(a =>
        a.Name != null &&
        a.Name.Trim().ToLower() == desiredKey   // EF dịch thành LTRIM/RTRIM + LOWER
    );

                    var role = string.IsNullOrWhiteSpace(na.Role) ? "Diễn viên" : na.Role.Trim();

                    if (sys != null)
                    {
                        // Nếu submission đã link actor này rồi -> bỏ qua
                        var dupLink = await _context.MovieSubmissionActors
                            .AnyAsync(msa => msa.MovieSubmissionId == submissionId && msa.ActorId == sys.ActorId);
                        if (dupLink) continue;

                        toAdd.Add(new MovieSubmissionActor
                        {
                            MovieSubmissionId = submissionId,
                            ActorId = sys.ActorId,
                            ActorName = sys.Name,
                            ActorAvatarUrl = sys.AvatarUrl,
                            Role = role
                        });
                    }
                    else
                    {
                        // tạo draft (ActorId = null)
                        toAdd.Add(new MovieSubmissionActor
                        {
                            MovieSubmissionId = submissionId,
                            ActorId = null,
                            ActorName = desiredName,
                            ActorAvatarUrl = na.AvatarUrl,
                            Role = role
                        });
                    }

                    nameSet.Add(desiredKey);
                }
            }

            if (toAdd.Count > 0)
                _context.MovieSubmissionActors.AddRange(toAdd);
            // KHÔNG SaveChanges ở đây — caller sẽ save 1 lần
        }

        public async Task<MovieSubmissionResponse> UpdateMovieSubmissionAsync(
     int submissionId, int partnerId, UpdateMovieSubmissionRequest request)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var submission = await ValidateSubmissionAccessAsync(submissionId, partnerId);

                // Validate các trường scalar nếu có
                if (!string.IsNullOrWhiteSpace(request.Title) ||
                    request.DurationMinutes.HasValue ||
                    !string.IsNullOrWhiteSpace(request.PosterUrl) ||
                    !string.IsNullOrWhiteSpace(request.BannerUrl) ||
                    !string.IsNullOrWhiteSpace(request.TrailerUrl) ||
                    request.PremiereDate.HasValue ||
                    request.EndDate.HasValue)
                {
                    await ValidateUpdateRequestAsync(request);
                }

                // Gán các field scalar
                if (!string.IsNullOrWhiteSpace(request.Title)) submission.Title = request.Title.Trim();
                if (!string.IsNullOrWhiteSpace(request.Genre)) submission.Genre = request.Genre.Trim();
                if (request.DurationMinutes.HasValue) submission.DurationMinutes = request.DurationMinutes.Value;
                if (!string.IsNullOrWhiteSpace(request.Director)) submission.Director = request.Director.Trim();
                if (!string.IsNullOrWhiteSpace(request.Language)) submission.Language = request.Language.Trim();
                if (!string.IsNullOrWhiteSpace(request.Country)) submission.Country = request.Country.Trim();
                if (request.PosterUrl != null) submission.PosterUrl = request.PosterUrl;
                if (request.BannerUrl != null) submission.BannerUrl = request.BannerUrl;
                if (request.Production != null) submission.Production = request.Production.Trim();
                if (request.Description != null) submission.Description = request.Description.Trim();
                if (request.PremiereDate.HasValue) submission.PremiereDate = request.PremiereDate.Value;
                if (request.EndDate.HasValue) submission.EndDate = request.EndDate.Value;
                if (request.TrailerUrl != null) submission.TrailerUrl = request.TrailerUrl;
                if (request.CopyrightDocumentUrl != null) submission.CopyrightDocumentUrl = request.CopyrightDocumentUrl;
                if (request.DistributionLicenseUrl != null) submission.DistributionLicenseUrl = request.DistributionLicenseUrl;
                if (request.AdditionalNotes != null) submission.AdditionalNotes = request.AdditionalNotes.Trim();

                submission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return await GetMovieSubmissionResponseAsync(submissionId);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<SubmitMovieSubmissionResponse> SubmitMovieSubmissionAsync(int submissionId, int partnerId)
        {
            var submission = await ValidateSubmissionAccessAsync(submissionId, partnerId, allowRejected: true);

            // Không cho submit nếu đã bị reject do trùng phim
            if (submission.Status == "Rejected" &&
                string.Equals(submission.RejectionReason, "Đã có phim trùng", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("status", "Phim này đã tồn tại trong hệ thống. Không thể nộp lại.");
            }

            // Chuẩn hoá tiêu đề khi cần so khớp Movie có sẵn
            string key = NormalizeTitle(submission.Title);

            // (Tuỳ chọn) Nếu muốn chặn ngay khi submit khi Movie đã tồn tại:
            var existed = await _context.Movies.AnyAsync(m => NormalizeTitle(m.Title) == key);
            if (existed)
            {
                // Bạn chọn: báo 400/409 hay chuyển Rejected ngay. Ở đây mình trả Validation 409.
                throw new ConflictException("title", "Tiêu đề này đã tồn tại trong hệ thống.");
            }

            submission.Status = "Pending";
            submission.SubmittedAt = DateTime.UtcNow;

            if (submission.RejectionReason != null)
            {
                submission.ResubmitCount += 1;
                submission.ResubmittedAt = DateTime.UtcNow;
                submission.RejectionReason = null;
            }

            // Gán reviewer theo partner như bạn đã làm:
            var partner = await _context.Partners.Include(p => p.Manager)
                                 .FirstOrDefaultAsync(p => p.PartnerId == partnerId);
            if (partner?.ManagerId == null)
                throw new ValidationException("system", "Không tìm thấy manager cho partner này.");
            submission.ReviewerId = partner.ManagerId;

            submission.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new SubmitMovieSubmissionResponse { /* fill như cũ */ };
        }

        public async Task DeleteMovieSubmissionAsync(int submissionId, int partnerId)
        {
            var submission = await ValidateSubmissionAccessAsync(submissionId, partnerId);

            // Soft delete - remove actors first
            var actors = _context.MovieSubmissionActors
                .Where(msa => msa.MovieSubmissionId == submissionId);
            _context.MovieSubmissionActors.RemoveRange(actors);

            // Then remove submission
            _context.MovieSubmissions.Remove(submission);
            await _context.SaveChangesAsync();
        }

        // ==================== PRIVATE HELPER METHODS ====================

        // Đây là các hàm (method) trong file service của bạn
        // (Giả sử chúng nằm trong cùng một class)

        private async Task ValidateMovieSubmissionRequestAsync(CreateMovieSubmissionRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();
            var today = DateOnly.FromDateTime(DateTime.Today);

            _logger.LogInformation("[ValidateSubmission] START | title={Title}, director={Director}, premiere={Premiere}, end={End}",
        request.Title, request.Director, request.PremiereDate, request.EndDate);
            // ===== URL checks =====
            if (!string.IsNullOrWhiteSpace(request.PosterUrl) && !IsValidImageUrl(request.PosterUrl))
                errors["posterUrl"] = new ValidationError { Msg = "URL poster phải là ảnh hợp lệ (jpg, jpeg, png, webp)", Path = "posterUrl", Location = "body" };

            if (!string.IsNullOrWhiteSpace(request.BannerUrl) && !IsValidImageUrl(request.BannerUrl))
                errors["bannerUrl"] = new ValidationError { Msg = "URL banner phải là ảnh hợp lệ (jpg, jpeg, png, webp)", Path = "bannerUrl", Location = "body" };

            if (!string.IsNullOrWhiteSpace(request.TrailerUrl) && !IsValidYoutubeUrl(request.TrailerUrl))
                errors["trailerUrl"] = new ValidationError { Msg = "URL trailer phải là YouTube hợp lệ", Path = "trailerUrl", Location = "body" };

            if (string.IsNullOrWhiteSpace(request.CopyrightDocumentUrl))
                errors["copyrightDocumentUrl"] = new ValidationError { Msg = "Tài liệu bản quyền là bắt buộc", Path = "copyrightDocumentUrl", Location = "body" };
            else if (!IsValidDocumentUrl(request.CopyrightDocumentUrl!))
                errors["copyrightDocumentUrl"] = new ValidationError { Msg = "Tài liệu bản quyền phải là ảnh (jpg, jpeg, png, webp) hoặc PDF", Path = "copyrightDocumentUrl", Location = "body" };

            if (string.IsNullOrWhiteSpace(request.DistributionLicenseUrl))
                errors["distributionLicenseUrl"] = new ValidationError { Msg = "Giấy phép phân phối là bắt buộc", Path = "distributionLicenseUrl", Location = "body" };
            else if (!IsValidDocumentUrl(request.DistributionLicenseUrl!))
                errors["distributionLicenseUrl"] = new ValidationError { Msg = "Giấy phép phân phối phải là ảnh (jpg, jpeg, png, webp) hoặc PDF", Path = "distributionLicenseUrl", Location = "body" };
            if (string.IsNullOrWhiteSpace(request.Title))
                errors["title"] = new ValidationError { Msg = "Tiêu đề là bắt buộc", Path = "title", Location = "body" };
            else if (request.Title.Length > 200)
                errors["title"] = new ValidationError { Msg = "Tiêu đề tối đa 200 ký tự", Path = "title", Location = "body" };

            if (string.IsNullOrWhiteSpace(request.Genre))
                errors["genre"] = new ValidationError { Msg = "Thể loại là bắt buộc", Path = "genre", Location = "body" };

            if (string.IsNullOrWhiteSpace(request.Director))
                errors["director"] = new ValidationError { Msg = "Đạo diễn là bắt buộc", Path = "director", Location = "body" };

            if (string.IsNullOrWhiteSpace(request.Language))
                errors["language"] = new ValidationError { Msg = "Ngôn ngữ là bắt buộc", Path = "language", Location = "body" };

            if (string.IsNullOrWhiteSpace(request.Country))
                errors["country"] = new ValidationError { Msg = "Quốc gia là bắt buộc", Path = "country", Location = "body" };

            // ===== DateOnly (bắt buộc trong Create) =====
            // Model đã [Required] nên gần như chắc chắn có giá trị, nhưng vẫn kiểm tra business rule:
            if (request.PremiereDate < today)
                errors["premiereDate"] = new ValidationError { Msg = "Ngày công chiếu phải ở tương lai", Path = "premiereDate", Location = "body" };

            if (request.EndDate <= request.PremiereDate)
                errors["endDate"] = new ValidationError { Msg = "Ngày kết thúc phải sau ngày công chiếu", Path = "endDate", Location = "body" };

            // ===== Actor IDs tồn tại =====
            if (request.ActorIds?.Any() == true)
            {
                var askIds = request.ActorIds.Distinct().ToList();
                var existIds = await _context.Actors
                    .Where(a => askIds.Contains(a.ActorId))
                    .Select(a => a.ActorId)
                    .ToListAsync();

                var missing = askIds.Except(existIds).ToList();
                if (missing.Any())
                    errors["actorIds"] = new ValidationError
                    {
                        Msg = $"Không tìm thấy diễn viên với ID: {string.Join(", ", missing)}",
                        Path = "actorIds",
                        Location = "body"
                    };
            }

            // ===== NewActors validate đơn lẻ (format) =====
            if (request.NewActors?.Any() == true)
            {
                for (int i = 0; i < request.NewActors.Count; i++)
                {
                    var na = request.NewActors[i];
                    if (!string.IsNullOrWhiteSpace(na.AvatarUrl) && !IsValidImageUrl(na.AvatarUrl!))
                        errors[$"newActors[{i}].avatarUrl"] = new ValidationError { Msg = "Avatar phải là ảnh (jpg, jpeg, png, webp)", Path = $"newActors[{i}].avatarUrl", Location = "body" };
                }
            }

            if (errors.Any())
            {
                _logger.LogWarning("[ValidateSubmission] FAIL | errors={Errors}", JsonSerializer.Serialize(errors));
                throw new ValidationException(errors);
            }

            _logger.LogInformation("[ValidateSubmission] OK");
        }
        private async Task ValidateUpdateRequestAsync(UpdateMovieSubmissionRequest request)
        {
            var errors = new Dictionary<string, ValidationError>();
            var today = DateOnly.FromDateTime(DateTime.Today);

            // URLs nếu có
            if (!string.IsNullOrWhiteSpace(request.PosterUrl) && !IsValidImageUrl(request.PosterUrl))
                errors["posterUrl"] = new ValidationError { Msg = "URL poster phải là ảnh hợp lệ (jpg, jpeg, png, webp)", Path = "posterUrl", Location = "body" };

            if (!string.IsNullOrWhiteSpace(request.BannerUrl) && !IsValidImageUrl(request.BannerUrl))
                errors["bannerUrl"] = new ValidationError { Msg = "URL banner phải là ảnh hợp lệ (jpg, jpeg, png, webp)", Path = "bannerUrl", Location = "body" };

            if (!string.IsNullOrWhiteSpace(request.TrailerUrl) && !IsValidYoutubeUrl(request.TrailerUrl))
                errors["trailerUrl"] = new ValidationError { Msg = "URL trailer phải là YouTube hợp lệ", Path = "trailerUrl", Location = "body" };

            if (!string.IsNullOrWhiteSpace(request.CopyrightDocumentUrl) && !IsValidDocumentUrl(request.CopyrightDocumentUrl))
                errors["copyrightDocumentUrl"] = new ValidationError { Msg = "Tài liệu bản quyền phải là ảnh (jpg, jpeg, png, webp) hoặc PDF", Path = "copyrightDocumentUrl", Location = "body" };

            if (!string.IsNullOrWhiteSpace(request.DistributionLicenseUrl) && !IsValidDocumentUrl(request.DistributionLicenseUrl))
                errors["distributionLicenseUrl"] = new ValidationError { Msg = "Giấy phép phân phối phải là ảnh (jpg, jpeg, png, webp) hoặc PDF", Path = "distributionLicenseUrl", Location = "body" };

            // Dates nếu có
            if (request.PremiereDate.HasValue && request.PremiereDate.Value < today)
                errors["premiereDate"] = new ValidationError { Msg = "Ngày công chiếu phải ở tương lai", Path = "premiereDate", Location = "body" };

            if (request.EndDate.HasValue && request.PremiereDate.HasValue && request.EndDate.Value <= request.PremiereDate.Value)
                errors["endDate"] = new ValidationError { Msg = "Ngày kết thúc phải sau ngày công chiếu", Path = "endDate", Location = "body" };

            if (errors.Any())
                throw new ValidationException(errors);
        }
        private async Task ValidateSubmissionForSubmitAsync(MovieSubmission submission)
        {
            var errors = new Dictionary<string, ValidationError>();
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Tài liệu bắt buộc
            if (string.IsNullOrWhiteSpace(submission.CopyrightDocumentUrl))
                errors["copyrightDocumentUrl"] = new ValidationError { Msg = "Tài liệu bản quyền là bắt buộc để nộp phim", Path = "copyrightDocumentUrl", Location = "body" };

            if (string.IsNullOrWhiteSpace(submission.DistributionLicenseUrl))
                errors["distributionLicenseUrl"] = new ValidationError { Msg = "Giấy phép phân phối là bắt buộc để nộp phim", Path = "distributionLicenseUrl", Location = "body" };

            // Premiere/End Date (DB của bạn đang dùng DateOnly; nếu nullable thì đổi sang HasValue)
            // Hỗ trợ cả 2 trường hợp: nullable hoặc không nullable
            bool hasPremiere = true;
            DateOnly premiere;
            try { premiere = submission.PremiereDate; }
            catch { hasPremiere = false; premiere = default; }

            bool hasEnd = true;
            DateOnly endDate;
            try { endDate = submission.EndDate; }
            catch { hasEnd = false; endDate = default; }

            if (!hasPremiere || premiere == default)
                errors["premiereDate"] = new ValidationError { Msg = "Ngày công chiếu là bắt buộc để nộp phim", Path = "premiereDate", Location = "body" };
            else if (premiere < today)
                errors["premiereDate"] = new ValidationError { Msg = "Ngày công chiếu phải ở tương lai", Path = "premiereDate", Location = "body" };

            if (hasPremiere && hasEnd && endDate != default && endDate <= premiere)
                errors["endDate"] = new ValidationError { Msg = "Ngày kết thúc phải sau ngày công chiếu", Path = "endDate", Location = "body" };

            if (errors.Any())
                throw new ValidationException(errors);
        }

        private async Task AddActorsToSubmissionAsync(int submissionId, CreateMovieSubmissionRequest request)
        {
            try
            {
                var toAdd = new List<MovieSubmissionActor>();

                // Lấy dữ liệu hiện có để chống trùng tên/ID
                var existing = await _context.MovieSubmissionActors
                    .Where(msa => msa.MovieSubmissionId == submissionId)
                    .Select(msa => new
                    {
                        msa.ActorId,
                        Name = msa.ActorId != null ? msa.Actor!.Name : msa.ActorName
                    })
                    .ToListAsync();

                static string Key(string? s) => (s ?? string.Empty).Trim().ToLower();

                var existingNameSet = existing.Select(x => Key(x.Name)).ToHashSet();
                var existingLinkedIds = existing.Where(x => x.ActorId.HasValue)
                                                .Select(x => x.ActorId!.Value)
                                                .ToHashSet();

                // 1) Link theo ActorIds
                if (request.ActorIds?.Any() == true)
                {
                    var linkIds = request.ActorIds.Distinct()
                                                .Where(id => !existingLinkedIds.Contains(id))
                                                .ToList();
                    if (linkIds.Any())
                    {
                        var actors = await _context.Actors
                            .Where(a => linkIds.Contains(a.ActorId))
                            .ToListAsync();

                        foreach (var a in actors)
                        {
                            var role = (request.ActorRoles != null &&
                                        request.ActorRoles.TryGetValue(a.ActorId, out var r) &&
                                        !string.IsNullOrWhiteSpace(r))
                                        ? r.Trim() : "Diễn viên";

                            // tránh trùng theo tên (đã có draft cùng tên)
                            if (existingNameSet.Contains(Key(a.Name))) continue;

                            toAdd.Add(new MovieSubmissionActor
                            {
                                MovieSubmissionId = submissionId,
                                ActorId = a.ActorId,
                                ActorName = a.Name,
                                ActorAvatarUrl = a.AvatarUrl,
                                Role = role
                            });

                            existingLinkedIds.Add(a.ActorId);
                            existingNameSet.Add(Key(a.Name));
                        }
                    }
                }

                // 2) Xử lý NewActors
                if (request.NewActors?.Any() == true)
                {
                    foreach (var na in request.NewActors)
                    {
                        var desiredName = (na.Name ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(desiredName)) continue;

                        var key = Key(desiredName);
                        var role = string.IsNullOrWhiteSpace(na.Role) ? "Diễn viên" : na.Role.Trim();

                        // bỏ nếu đã có trong submission (link/draft)
                        if (existingNameSet.Contains(key)) continue;

                        // nếu trùng actor hệ thống -> AUTO-LINK (PUT phải chạy được với JSON create)
                        var sys = await _context.Actors
                            .OrderBy(a => a.ActorId)
                            .FirstOrDefaultAsync(a =>
                                a.Name != null &&
                                a.Name.Trim().ToLower() == key);

                        if (sys != null)
                        {
                            // nếu đã link id này thì bỏ
                            var dupLink = await _context.MovieSubmissionActors
                                .AnyAsync(msa => msa.MovieSubmissionId == submissionId && msa.ActorId == sys.ActorId);
                            if (!dupLink)
                            {
                                toAdd.Add(new MovieSubmissionActor
                                {
                                    MovieSubmissionId = submissionId,
                                    ActorId = sys.ActorId,
                                    ActorName = sys.Name,
                                    ActorAvatarUrl = sys.AvatarUrl,
                                    Role = role
                                });
                                existingLinkedIds.Add(sys.ActorId);
                                existingNameSet.Add(key);
                            }
                            continue;
                        }

                        // không có trong hệ thống -> tạo draft
                        toAdd.Add(new MovieSubmissionActor
                        {
                            MovieSubmissionId = submissionId,
                            ActorId = null,
                            ActorName = desiredName,
                            ActorAvatarUrl = na.AvatarUrl,
                            Role = role
                        });

                        existingNameSet.Add(key);
                    }
                }

                if (toAdd.Count > 0)
                    _context.MovieSubmissionActors.AddRange(toAdd);

                // KHÔNG SaveChanges ở đây — caller sẽ commit 1 lần
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi trong AddActorsToSubmissionAsync: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }


        private async Task<MovieSubmissionResponse> GetMovieSubmissionResponseAsync(int submissionId)
        {
            _logger.LogInformation("[BuildResponse] START | submissionId={SubmissionId}", submissionId);

            var submission = await _context.MovieSubmissions
                .Include(ms => ms.MovieSubmissionActors)
                    .ThenInclude(msa => msa.Actor)
                .FirstOrDefaultAsync(ms => ms.MovieSubmissionId == submissionId);

            if (submission == null)
            {
                _logger.LogWarning("[BuildResponse] NOT_FOUND | submissionId={SubmissionId}", submissionId);
                throw new NotFoundException("Không tìm thấy bản nháp phim");
            }

            var resp = await MapToMovieSubmissionResponseAsync(submission);
            _logger.LogInformation("[BuildResponse] OK | status={Status}, actors={ActorCount}",
                resp.Status, resp.Actors?.Count ?? 0);

            return resp;
        }

        private async Task<MovieSubmissionResponse> MapToMovieSubmissionResponseAsync(MovieSubmission submission)
        {
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

        // ==================== PRIVATE VALIDATION METHODS ====================
        private static string SimpleN(string? s) => (s ?? "").Trim().ToLowerInvariant();

        private async Task<MovieSubmission> ValidateSubmissionAccessAsync(
    int submissionId,
    int partnerId,
    bool allowRejected = false)
        {
            var submission = await _context.MovieSubmissions
                .FirstOrDefaultAsync(ms => ms.MovieSubmissionId == submissionId
                                        && ms.PartnerId == partnerId);

            if (submission == null)
                throw new NotFoundException("Không tìm thấy movie submission");

            var isDraft = submission.Status == SubmissionStatus.Draft.ToString();
            var isRejected = submission.Status == SubmissionStatus.Rejected.ToString();

            // Mặc định chỉ cho thao tác khi Draft.
            // Cho phép thêm khi Rejected nếu allowRejected = true (để sửa & nộp lại).
            if (!isDraft && !(allowRejected && isRejected))
            {
                throw new ValidationException(new Dictionary<string, ValidationError>
                {
                    ["status"] = new ValidationError
                    {
                        Msg = "Chỉ được thao tác khi submission ở trạng thái Draft"
                              + (allowRejected ? " hoặc Rejected." : "."),
                        Path = "status",
                        Location = "body"
                    }
                });
            }

            return submission;
        }
        private async Task ValidateDuplicateActorInSubmissionAsync(int submissionId, string actorName)
        {
            var n = (actorName ?? "").Trim().ToLower();

            var dup = await (
                from msa in _context.MovieSubmissionActors
                join a in _context.Actors on msa.ActorId equals a.ActorId into aj
                from a in aj.DefaultIfEmpty()
                where msa.MovieSubmissionId == submissionId
                let name = msa.ActorId != null ? (a.Name ?? "") : (msa.ActorName ?? "")
                select name
            ).AnyAsync(x => x.Trim().ToLower() == n);

            if (dup)
                throw new ConflictException("actorName", "Diễn viên đã có trong submission này");
        }
        private SubmissionActorResponse MapToSubmissionActorResponse(MovieSubmissionActor submissionActor)
        {
            var actorName = submissionActor.ActorId.HasValue && submissionActor.Actor != null
                ? submissionActor.Actor.Name
                : submissionActor.ActorName ?? string.Empty;

            var actorAvatarUrl = submissionActor.ActorId.HasValue && submissionActor.Actor != null
                ? submissionActor.Actor.AvatarUrl
                : submissionActor.ActorAvatarUrl;

            return new SubmissionActorResponse
            {
                MovieSubmissionActorId = submissionActor.MovieSubmissionActorId,
                ActorId = submissionActor.ActorId,
                ActorName = actorName,
                ActorAvatarUrl = actorAvatarUrl,
                Role = submissionActor.Role,
                IsExistingActor = submissionActor.ActorId.HasValue
            };
        }

        private IQueryable<Actor> ApplyActorSorting(IQueryable<Actor> query, string? sortBy, string? sortOrder)
        {
            sortBy = sortBy?.ToLower() ?? "name";
            sortOrder = sortOrder?.ToLower() ?? "asc";

            var isAscending = sortOrder == "asc";

            return sortBy switch
            {
                "actor_id" => isAscending ? query.OrderBy(a => a.ActorId) : query.OrderByDescending(a => a.ActorId),
                "name" => isAscending ? query.OrderBy(a => a.Name) : query.OrderByDescending(a => a.Name),
                _ => isAscending ? query.OrderBy(a => a.Name) : query.OrderByDescending(a => a.Name)
            };
        }

        private IQueryable<MovieSubmission> ApplySubmissionSorting(IQueryable<MovieSubmission> query, string? sortBy, string? sortOrder)
        {
            sortBy = sortBy?.ToLower() ?? "createdat";
            sortOrder = sortOrder?.ToLower() ?? "desc";

            var isAscending = sortOrder == "asc";

            return sortBy switch
            {
                "title" => isAscending ? query.OrderBy(ms => ms.Title) : query.OrderByDescending(ms => ms.Title),
                "createdat" => isAscending ? query.OrderBy(ms => ms.CreatedAt) : query.OrderByDescending(ms => ms.CreatedAt),
                "updatedat" => isAscending ? query.OrderBy(ms => ms.UpdatedAt) : query.OrderByDescending(ms => ms.UpdatedAt),
                "submittedat" => isAscending ? query.OrderBy(ms => ms.SubmittedAt) : query.OrderByDescending(ms => ms.SubmittedAt),
                "status" => isAscending ? query.OrderBy(ms => ms.Status) : query.OrderByDescending(ms => ms.Status),
                _ => isAscending ? query.OrderBy(ms => ms.CreatedAt) : query.OrderByDescending(ms => ms.CreatedAt)
            };
        }

        private bool IsValidImageUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
                && System.Text.RegularExpressions.Regex.IsMatch(uriResult.AbsolutePath, @"\.(jpg|jpeg|png|webp)$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private bool IsValidYoutubeUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                && (uriResult.Host.Contains("youtube.com") || uriResult.Host.Contains("youtu.be"));
        }

        private bool IsValidDocumentUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
                && System.Text.RegularExpressions.Regex.IsMatch(uriResult.AbsolutePath, @"\.(jpg|jpeg|png|webp|pdf)$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        private static string NormalizeTitle(string? title)
        {
            if (string.IsNullOrWhiteSpace(title)) return string.Empty;

            // Trim + gộp nhiều khoảng trắng thành 1
            var s = Regex.Replace(title.Trim(), @"\s+", " ");

            // Hạ lowercase
            s = s.ToLowerInvariant();

            // Bỏ dấu (unicode diacritics)
            var nf = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(nf.Length);
            foreach (var ch in nf)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            s = sb.ToString().Normalize(NormalizationForm.FormC);

            return s;
        }
    }
}