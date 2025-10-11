using System.ComponentModel.DataAnnotations;

namespace ExpressTicketCinemaSystem.Src.Cinema.Contracts.Auth.Request
{
    public class RegisterRequest
    {
        [Required]
        [MinLength(2, ErrorMessage = "Full name must be at least 2 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9_]{8,15}$",
            ErrorMessage = "Username must be 8–15 characters and can only contain letters, numbers, and underscores.")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,12}$",
            ErrorMessage = "Password must be 6–12 characters, include uppercase, lowercase, number, and special character.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
