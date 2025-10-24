using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Movie
{
    public int MovieId { get; set; }

    public string Title { get; set; } = null!;

    public string? Genre { get; set; }

    public int DurationMinutes { get; set; }

    public string? Director { get; set; }

    public string? Language { get; set; }

    public string? Country { get; set; }

    public bool IsActive { get; set; }

    public string? PosterUrl { get; set; }

    public string? Production { get; set; }

    public string? Description { get; set; }

    public DateOnly PremiereDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string? TrailerUrl { get; set; }

    public decimal? AverageRating { get; set; }

    public int? RatingsCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public int ManagerId { get; set; }
    public virtual Manager Manager { get; set; } = null!;

    public virtual ICollection<MovieActor> MovieActors { get; set; } = new List<MovieActor>();

    public virtual ICollection<RatingFilm> RatingFilms { get; set; } = new List<RatingFilm>();

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}