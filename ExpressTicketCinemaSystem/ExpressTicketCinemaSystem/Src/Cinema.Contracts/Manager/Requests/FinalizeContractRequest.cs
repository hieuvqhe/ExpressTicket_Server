namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests
{
    namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Manager.Requests
    {
        public class FinalizeContractRequest
        {
            public string ManagerSignature { get; set; } = string.Empty;
            public string? Notes { get; set; } 
        }
    }
}
