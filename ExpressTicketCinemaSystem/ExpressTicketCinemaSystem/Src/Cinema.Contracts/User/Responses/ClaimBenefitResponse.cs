namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.User.Responses;

public class ClaimBenefitResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public int? VoucherId { get; set; }
    public decimal? ClaimValue { get; set; }
}


















