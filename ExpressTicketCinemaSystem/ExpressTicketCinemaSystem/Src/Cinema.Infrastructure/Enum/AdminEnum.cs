namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Enum
{
    public class AdminEnum
    {
        /// <summary>
        /// Role: Available values = customer, employee, partner, manager, admin
        /// </summary>
        public enum UserRoleFilter
        {
            customer,
            employee,
            partner,
            manager,
            admin
        }

        /// <summary>
        /// Filter by verification status (0=unverified, 1=verified, 2=banned)
        /// </summary>
        public enum VerifyStatus
        {
            unverified = 0,
            verified = 1,
            banned = 2
        }

        /// <summary>
        /// Sort order options (asc, desc)
        /// </summary>
        public enum SortOrder
        {
            asc,
            desc
        }
    }

    /// <summary>
    /// Helper class to provide role options for dropdown
    /// </summary>
    public static class RoleDropdownHelper
    {
        public static List<RoleOption> GetRoleOptions()
        {
            return new List<RoleOption>
            {
                new RoleOption { Value = "customer", Label = "Customer" },
                new RoleOption { Value = "employee", Label = "employee" },
                new RoleOption { Value = "partner", Label = "Partner" },
                new RoleOption { Value = "manager", Label = "Manager" },
                new RoleOption { Value = "admin", Label = "Admin" }
            };
        }

        public static List<VerifyStatusOption> GetVerifyStatusOptions()
        {
            return new List<VerifyStatusOption>
            {
                new VerifyStatusOption { Value = 0, Label = "Unverified" },
                new VerifyStatusOption { Value = 1, Label = "Verified" },
                new VerifyStatusOption { Value = 2, Label = "Banned" }
            };
        }

        // Thêm method mới cho sort order options
        public static List<SortOrderOption> GetSortOrderOptions()
        {
            return new List<SortOrderOption>
            {
                new SortOrderOption { Value = "asc", Label = "Ascending" },
                new SortOrderOption { Value = "desc", Label = "Descending" }
            };
        }
    }

    public class RoleOption
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class VerifyStatusOption
    {
        public int Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    // Thêm class mới cho sort order option
    public class SortOrderOption
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
    public enum VerifyStatus
    {
        Unverified = 0,  // !EmailConfirmed
        Verified = 1,    // EmailConfirmed && IsActive && !IsBanned
        Banned = 2       // IsBanned
    }
}