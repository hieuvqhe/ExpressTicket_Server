namespace ExpressTicketCinemaSystem.Src.Cinema.Application.Services
{
    public class AzureBlobStorageSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerName { get; set; } = "contracts";
        public int SasTokenExpiryMinutes { get; set; } = 10;
    }
}