using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using ExpressTicketCinemaSystem.Src.Cinema.Application.Exceptions;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Catalog.Requests;
using ExpressTicketCinemaSystem.Src.Cinema.Contracts.Catalog.Responses;
using ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class CatalogQueryService : ICatalogQueryService
    {
        private readonly CinemaDbCoreContext _db;
        public CatalogQueryService(CinemaDbCoreContext db) { _db = db; }

        public async Task<MovieShowtimesOverviewResponse> GetMovieShowtimesOverviewAsync(
            int movieId, GetMovieShowtimesOverviewQuery query, CancellationToken ct = default)
        {
            // 1) Validate input
            if (string.IsNullOrWhiteSpace(query.Date))
                throw new ValidationException("date", "Thiếu tham số ngày (yyyy-MM-dd)");

            if (!DateOnly.TryParseExact(query.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var dateOnly))
                throw new ValidationException("date", "Định dạng ngày không hợp lệ (yyyy-MM-dd)");

            var movie = await _db.Movies.AsNoTracking()
                .Where(m => m.MovieId == movieId && m.IsActive)
                .Select(m => new { m.MovieId, m.Title, m.PosterUrl })
                .FirstOrDefaultAsync(ct);

            if (movie == null) throw new NotFoundException("Không tìm thấy phim");

            // 2) Build query showtime theo ngày + filter
            var start = dateOnly.ToDateTime(TimeOnly.MinValue);
            var end = dateOnly.ToDateTime(TimeOnly.MaxValue);

            var q = _db.Showtimes
                .AsNoTracking()
                .Where(s => s.MovieId == movieId && s.ShowDatetime >= start && s.ShowDatetime <= end);

            if (!string.IsNullOrWhiteSpace(query.City))
                q = q.Where(s => s.Cinema.City == query.City);

            if (!string.IsNullOrWhiteSpace(query.District))
                q = q.Where(s => s.Cinema.District == query.District);

            if (!string.IsNullOrWhiteSpace(query.Brand))
                q = q.Where(s => s.Cinema.Code.StartsWith(query.Brand)); // ví dụ: CGV_*, LOTTE_*

            if (query.CinemaId.HasValue)
                q = q.Where(s => s.CinemaId == query.CinemaId.Value);

            if (!string.IsNullOrWhiteSpace(query.ScreenType))
                q = q.Where(s => s.Screen.ScreenType == query.ScreenType);

            if (!string.IsNullOrWhiteSpace(query.FormatType))
                q = q.Where(s => s.FormatType == query.FormatType);

            if (!string.IsNullOrWhiteSpace(query.TimeFrom) && TimeOnly.TryParseExact(query.TimeFrom, "HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var from))
                q = q.Where(s => TimeOnly.FromDateTime(s.ShowDatetime) >= from);

            if (!string.IsNullOrWhiteSpace(query.TimeTo) && TimeOnly.TryParseExact(query.TimeTo, "HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var to))
                q = q.Where(s => TimeOnly.FromDateTime(s.ShowDatetime) <= to);

            // 3) Sort cơ bản ở cấp showtime để khi group vẫn ổn
            q = (query.SortBy?.ToLowerInvariant(), query.SortOrder?.ToLowerInvariant()) switch
            {
                ("cinema", "desc") => q.OrderByDescending(s => s.Cinema.CinemaName).ThenBy(s => s.ShowDatetime),
                ("cinema", _) => q.OrderBy(s => s.Cinema.CinemaName).ThenBy(s => s.ShowDatetime),

                ("brand", "desc") => q.OrderByDescending(s => s.Cinema.Code).ThenBy(s => s.ShowDatetime),
                ("brand", _) => q.OrderBy(s => s.Cinema.Code).ThenBy(s => s.ShowDatetime),

                ("time", "desc") => q.OrderByDescending(s => s.ShowDatetime),
                _ => q.OrderBy(s => s.ShowDatetime)
            };

            var raw = await q.Select(s => new
            {
                s.ShowtimeId,
                s.ShowDatetime,
                s.EndTime,
                s.BasePrice,
                s.FormatType,
                s.AvailableSeats,
                Cinema = new { s.Cinema.CinemaId, s.Cinema.CinemaName, s.Cinema.Address, s.Cinema.City, s.Cinema.District, s.Cinema.Code, s.Cinema.LogoUrl },
                Screen = new { s.Screen.ScreenId, s.Screen.ScreenName, s.Screen.ScreenType, s.Screen.SoundSystem, s.Screen.Capacity }
            }).ToListAsync(ct);

            // 4) Brands khả dụng cho ngày đó
            var brands = raw.Select(x => x.Cinema.Code?.Split('_').FirstOrDefault() ?? "")
                            .Where(b => !string.IsNullOrEmpty(b))
                            .Distinct()
                            .OrderBy(b => b)
                            .Select(b => new BrandItem { Code = b, Name = b, LogoUrl = null })
                            .ToList();

            // 5) Group theo rạp → phòng → showtimes
            var cinemaGroups = raw.GroupBy(x => x.Cinema).ToList();

            // 6) Pagination ở cấp rạp
            int page = query.Page < 1 ? 1 : query.Page;
            int limit = (query.Limit < 1 || query.Limit > 100) ? 10 : query.Limit;
            int total = cinemaGroups.Count;
            int totalPages = (int)Math.Ceiling(total / (double)limit);
            var pageCinemas = cinemaGroups.Skip((page - 1) * limit).Take(limit);

            var cinemaItems = new List<MovieShowtimeCinemaGroup>();
            foreach (var cGrp in pageCinemas)
            {
                var c = new MovieShowtimeCinemaGroup
                {
                    CinemaId = cGrp.Key.CinemaId,
                    CinemaName = cGrp.Key.CinemaName,
                    Address = cGrp.Key.Address ?? "",
                    City = cGrp.Key.City ?? "",
                    District = cGrp.Key.District ?? "",
                    BrandCode = cGrp.Key.Code?.Split('_').FirstOrDefault() ?? "",
                    LogoUrl = cGrp.Key.LogoUrl
                };

                foreach (var sGrp in cGrp.GroupBy(x => x.Screen))
                {
                    var sc = new MovieShowtimeScreenGroup
                    {
                        ScreenId = sGrp.Key.ScreenId,
                        ScreenName = sGrp.Key.ScreenName,
                        ScreenType = sGrp.Key.ScreenType ?? "",
                        SoundSystem = sGrp.Key.SoundSystem ?? "",
                        Capacity = sGrp.Key.Capacity ?? 0
                    };

                    sc.Showtimes = sGrp.Select(x => new MovieShowtimeItem
                    {
                        ShowtimeId = x.ShowtimeId,
                        StartTime = x.ShowDatetime,
                        EndTime = x.EndTime,
                        FormatType = x.FormatType ?? "",
                        BasePrice = x.BasePrice,
                        AvailableSeats = x.AvailableSeats ?? 0,            
                        IsSoldOut = (x.AvailableSeats ?? 0) <= 0,     
                        Label = string.IsNullOrEmpty(x.FormatType) ? "Suất chiếu" : x.FormatType
                    }).ToList();

                    c.Screens.Add(sc);
                }

                cinemaItems.Add(c);
            }

            var response = new MovieShowtimesOverviewResponse
            {
                MovieId = movie.MovieId,
                Title = movie.Title,
                PosterUrl = movie.PosterUrl ?? "",
                Date = dateOnly,
                Brands = brands,
                Cinemas = new PaginatedCinemas
                {
                    Items = cinemaItems,
                    Pagination = new PaginationMeta
                    {
                        CurrentPage = page,
                        PageSize = limit,
                        TotalCount = total,
                        TotalPages = totalPages
                    }
                }
            };

            return response;
        }

        public async Task<ShowtimeSeatMapResponse> GetShowtimeSeatMapAsync(int showtimeId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var show = await _db.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Cinema)
                .Include(s => s.Screen)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId, ct);

            if (show == null) throw new NotFoundException("Không tìm thấy suất chiếu");

            var seatMap = await _db.SeatMaps.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ScreenId == show.ScreenId, ct);

            var totalRows = seatMap?.TotalRows ?? show.Screen.SeatRows ?? 0;
            var totalCols = seatMap?.TotalColumns ?? show.Screen.SeatColumns ?? 0;

            var seatTypes = await _db.SeatTypes.AsNoTracking()
                .Where(st => st.Status && st.PartnerId == show.Cinema.PartnerId)
                .Select(st => new SeatTypeInfo
                {
                    SeatTypeId = st.Id,
                    Code = st.Code,
                    Name = st.Name,
                    Surcharge = st.Surcharge,
                    Color = st.Color
                })
                .ToListAsync(ct);

            var seats = await _db.Seats.AsNoTracking()
                .Where(se => se.ScreenId == show.ScreenId)
                .Select(se => new { se.SeatId, se.RowCode, se.SeatNumber, se.SeatTypeId, se.Status })
                .ToListAsync(ct);

            var locks = await _db.SeatLocks.AsNoTracking()
                .Where(l => l.ShowtimeId == showtimeId && l.LockedUntil > now)
                .Select(l => new { l.SeatId, l.LockedUntil })
                .ToListAsync(ct);
            var lockMap = locks.ToDictionary(x => x.SeatId, x => x.LockedUntil);

            var sold = await _db.Tickets.AsNoTracking()
                .Where(t => t.ShowtimeId == showtimeId && (t.Status == "VALID" || t.Status == "USED"))
                .Select(t => t.SeatId)
                .ToListAsync(ct);
            var soldSet = sold.ToHashSet();

            var resp = new ShowtimeSeatMapResponse
            {
                ShowtimeId = showtimeId,
                Movie = new MovieBrief { MovieId = show.MovieId, Title = show.Movie.Title, PosterUrl = show.Movie.PosterUrl ?? "" },
                Cinema = new CinemaBrief { CinemaId = show.CinemaId, CinemaName = show.Cinema.CinemaName, City = show.Cinema.City ?? "", District = show.Cinema.District ?? "" },
                Screen = new ScreenBrief { ScreenId = show.ScreenId, ScreenName = show.Screen.ScreenName, ScreenType = show.Screen.ScreenType ?? "", SoundSystem = show.Screen.SoundSystem ?? "" },
                TotalRows = totalRows,
                TotalColumns = totalCols,
                SeatTypes = seatTypes,
                ServerTime = now
            };

            foreach (var s in seats.OrderBy(x => x.RowCode).ThenBy(x => x.SeatNumber))
            {
                var status = s.Status == "Blocked" ? "BLOCKED"
                           : soldSet.Contains(s.SeatId) ? "SOLD"
                           : lockMap.ContainsKey(s.SeatId) ? "LOCKED"
                           : "AVAILABLE";

                resp.Seats.Add(new SeatCell
                {
                    SeatId = s.SeatId,
                    RowCode = s.RowCode,
                    SeatNumber = s.SeatNumber,
                    SeatTypeId = s.SeatTypeId ?? 0,
                    Status = status,
                    LockedUntil = lockMap.TryGetValue(s.SeatId, out var lu) ? lu : null
                });
            }

            return resp;
        }
        public async Task<CinemaShowtimesResponse> GetCinemaShowtimesAsync(
    GetCinemaShowtimesQuery query, CancellationToken ct = default)
        {
            // 1) Validate date
            if (string.IsNullOrWhiteSpace(query.Date))
                throw new ValidationException("date", "Thiếu tham số ngày (yyyy-MM-dd)");

            if (!DateOnly.TryParseExact(query.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var dateOnly))
                throw new ValidationException("date", "Định dạng ngày không hợp lệ (yyyy-MM-dd)");

            var start = dateOnly.ToDateTime(TimeOnly.MinValue);
            var end = dateOnly.ToDateTime(TimeOnly.MaxValue);

            // 2) Base query
            var q = _db.Showtimes
                .AsNoTracking()
                .Where(s => s.ShowDatetime >= start && s.ShowDatetime <= end && s.Movie.IsActive);

            // 3) Apply filters
            if (query.MovieId.HasValue)
                q = q.Where(s => s.MovieId == query.MovieId.Value);

            if (!string.IsNullOrWhiteSpace(query.City))
                q = q.Where(s => s.Cinema.City == query.City);

            if (!string.IsNullOrWhiteSpace(query.District))
                q = q.Where(s => s.Cinema.District == query.District);

            if (!string.IsNullOrWhiteSpace(query.Brand))
                q = q.Where(s => s.Cinema.Code.StartsWith(query.Brand));

            if (!string.IsNullOrWhiteSpace(query.ScreenType))
                q = q.Where(s => s.Screen.ScreenType == query.ScreenType);

            if (!string.IsNullOrWhiteSpace(query.FormatType))
                q = q.Where(s => s.FormatType == query.FormatType);

            if (!string.IsNullOrWhiteSpace(query.TimeFrom) && TimeOnly.TryParseExact(query.TimeFrom, "HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var from))
                q = q.Where(s => TimeOnly.FromDateTime(s.ShowDatetime) >= from);

            if (!string.IsNullOrWhiteSpace(query.TimeTo) && TimeOnly.TryParseExact(query.TimeTo, "HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var to))
                q = q.Where(s => TimeOnly.FromDateTime(s.ShowDatetime) <= to);

            // 4) Sort
            q = (query.SortBy?.ToLowerInvariant(), query.SortOrder?.ToLowerInvariant()) switch
            {
                ("cinema", "desc") => q.OrderByDescending(s => s.Cinema.CinemaName).ThenBy(s => s.ShowDatetime),
                ("cinema", _) => q.OrderBy(s => s.Cinema.CinemaName).ThenBy(s => s.ShowDatetime),

                ("movie", "desc") => q.OrderByDescending(s => s.Movie.Title).ThenBy(s => s.ShowDatetime),
                ("movie", _) => q.OrderBy(s => s.Movie.Title).ThenBy(s => s.ShowDatetime),

                ("time", "desc") => q.OrderByDescending(s => s.ShowDatetime),
                _ => q.OrderBy(s => s.ShowDatetime)
            };

            // 5) Get data
            var raw = await q.Select(s => new
            {
                s.ShowtimeId,
                s.ShowDatetime,
                s.EndTime,
                s.BasePrice,
                s.FormatType,
                s.AvailableSeats,
                Cinema = new
                {
                    s.Cinema.CinemaId,
                    s.Cinema.CinemaName,
                    s.Cinema.Address,
                    s.Cinema.City,
                    s.Cinema.District,
                    s.Cinema.Code,
                    s.Cinema.LogoUrl
                },
                Screen = new
                {
                    s.Screen.ScreenId,
                    s.Screen.ScreenName,
                    s.Screen.ScreenType,
                    s.Screen.SoundSystem
                },
                Movie = new
                {
                    s.Movie.MovieId,
                    s.Movie.Title,
                    s.Movie.PosterUrl,
                    s.Movie.DurationMinutes,
                }
            }).ToListAsync(ct);

            // 6) Get available brands
            var brands = raw.Select(x => x.Cinema.Code?.Split('_').FirstOrDefault() ?? "")
                            .Where(b => !string.IsNullOrEmpty(b))
                            .Distinct()
                            .OrderBy(b => b)
                            .Select(b => new BrandItem { Code = b, Name = b, LogoUrl = null })
                            .ToList();

            // 7) Group by Cinema → Movie → Screen
            var cinemaGroups = raw.GroupBy(x => x.Cinema).ToList();

            // 8) Pagination
            int page = query.Page < 1 ? 1 : query.Page;
            int limit = (query.Limit < 1 || query.Limit > 100) ? 10 : query.Limit;
            int total = cinemaGroups.Count();
            int totalPages = (int)Math.Ceiling(total / (double)limit);
            var pageCinemas = cinemaGroups.Skip((page - 1) * limit).Take(limit);

            var cinemaItems = new List<CinemaShowtimeGroup>();

            foreach (var cinemaGroup in pageCinemas)
            {
                var cinemaItem = new CinemaShowtimeGroup
                {
                    CinemaId = cinemaGroup.Key.CinemaId,
                    CinemaName = cinemaGroup.Key.CinemaName,
                    Address = cinemaGroup.Key.Address ?? "",
                    City = cinemaGroup.Key.City ?? "",
                    District = cinemaGroup.Key.District ?? "",
                    BrandCode = cinemaGroup.Key.Code?.Split('_').FirstOrDefault() ?? "",
                    LogoUrl = cinemaGroup.Key.LogoUrl
                };

                // Group by Movie within this cinema
                var movieGroups = cinemaGroup.GroupBy(x => x.Movie);

                foreach (var movieGroup in movieGroups)
                {
                    var movieItem = new MovieShowtimeGroup
                    {
                        MovieId = movieGroup.Key.MovieId,
                        Title = movieGroup.Key.Title,
                        PosterUrl = movieGroup.Key.PosterUrl ?? "",
                        Duration = movieGroup.Key.DurationMinutes.ToString()
                    };

                    // Group by Screen within this movie
                    var screenGroups = movieGroup.GroupBy(x => x.Screen);

                    foreach (var screenGroup in screenGroups)
                    {
                        var screenItem = new CinemaScreenShowtimeGroup
                        {
                            ScreenId = screenGroup.Key.ScreenId,
                            ScreenName = screenGroup.Key.ScreenName,
                            ScreenType = screenGroup.Key.ScreenType ?? "",
                            SoundSystem = screenGroup.Key.SoundSystem ?? ""
                        };

                        screenItem.Showtimes = screenGroup.Select(x => new ShowtimeBriefItem
                        {
                            ShowtimeId = x.ShowtimeId,
                            StartTime = x.ShowDatetime,
                            EndTime = x.EndTime,
                            FormatType = x.FormatType ?? "",
                            BasePrice = x.BasePrice,
                            AvailableSeats = x.AvailableSeats ?? 0,
                            IsSoldOut = (x.AvailableSeats ?? 0) <= 0,
                            Label = string.IsNullOrEmpty(x.FormatType) ? "2D" : x.FormatType
                        }).ToList();

                        movieItem.Screens.Add(screenItem);
                    }

                    cinemaItem.Movies.Add(movieItem);
                }

                cinemaItems.Add(cinemaItem);
            }

            var response = new CinemaShowtimesResponse
            {
                Date = dateOnly,
                Brands = brands,
                Cinemas = new PaginatedCinemaShowtimes
                {
                    Items = cinemaItems,
                    Pagination = new PaginationMeta
                    {
                        CurrentPage = page,
                        PageSize = limit,
                        TotalCount = total,
                        TotalPages = totalPages
                    }
                }
            };

            return response;
        }
    }
}
