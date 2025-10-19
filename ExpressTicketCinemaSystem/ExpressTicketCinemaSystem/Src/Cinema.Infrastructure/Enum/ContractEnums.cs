namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Enum
{
    public static class ContractStatus
    {
        public const string Draft = "draft";
        public const string Pending = "pending";
        public const string Active = "active";
        public const string Expired = "expired";
        public const string Terminated = "terminated";
    }

    public static class ContractType
    {
        public const string Partnership = "partnership";
        public const string Service = "service";
        public const string Standard = "standard";
        public const string Premium = "premium";
    }

    public static class ContractSortFields
    {
        public const string ContractNumber = "contract_number";
        public const string Title = "title";
        public const string StartDate = "start_date";
        public const string EndDate = "end_date";
        public const string CommissionRate = "commission_rate";
        public const string Status = "status";
        public const string CreatedAt = "created_at";
        public const string UpdatedAt = "updated_at";
        public const string PartnerName = "partner_name";
    }

}
