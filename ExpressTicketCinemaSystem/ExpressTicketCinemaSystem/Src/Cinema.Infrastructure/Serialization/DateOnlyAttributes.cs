using System.ComponentModel.DataAnnotations;
namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Serialization
{
    public sealed class FutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            if (value is DateOnly date)
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                if (date < today) return new ValidationResult(ErrorMessage);
            }
            // null => để attribute bắt buộc ở nơi khác xử lý
            return ValidationResult.Success;
        }
    }

    // [DateAfter("PremiereDate")] cho DateOnly
    public sealed class DateAfterAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public DateAfterAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not DateOnly endDate) return ValidationResult.Success;

            var prop = validationContext.ObjectType.GetProperty(_comparisonProperty);
            if (prop == null) return ValidationResult.Success;

            var startObj = prop.GetValue(validationContext.ObjectInstance);
            if (startObj is DateOnly startDate && endDate <= startDate)
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}
