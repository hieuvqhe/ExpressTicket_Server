namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Admin.Responses
{
    public class ErrorAdminResponse
    {
        public string Message { get; set; } = string.Empty;
        public ErrorInfo ErrorInfo { get; set; } = new();
    }

    /// <summary>
    /// Error info model
    /// </summary>
    public class ErrorInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}