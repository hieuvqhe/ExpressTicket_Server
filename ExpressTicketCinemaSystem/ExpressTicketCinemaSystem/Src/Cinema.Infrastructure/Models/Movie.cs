using System;
using System.Collections.Generic;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Models;

public partial class Movie
{
    public int MovieId { get; set; }

    public string Title { get; set; } = null!;

    public string? Genre { get; set; }

    public int DurationMinutes { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? Director { get; set; }

    public string? Language { get; set; }

    public string? Country { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<MovieSubmission> MovieSubmissions { get; set; } = new List<MovieSubmission>();

    public virtual ICollection<RatingFilm> RatingFilms { get; set; } = new List<RatingFilm>();

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
