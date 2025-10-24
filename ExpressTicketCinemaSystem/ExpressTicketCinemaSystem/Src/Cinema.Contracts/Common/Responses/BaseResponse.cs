namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Common.Responses
{

    public class SuccessResponse<T>
    {
        public string Message { get; set; } = string.Empty;
        public T Result { get; set; }
    }

    public class ValidationErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, ValidationError> Errors { get; set; } = new();
    }

    public class ValidationError
    {
        public string Msg { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Location { get; set; } = "body";
    }
}