using System.ComponentModel.DataAnnotations;

namespace MP.Account
{
    public class UserProfileDto
    {
        public string? Name { get; set; }

        public string? Surname { get; set; }

        public string? Email { get; set; }

        [StringLength(50)]
        [RegularExpression(@"^(PL)?\d{26}$|^[A-Z]{2}\d{2}[A-Z0-9]{1,30}$",
            ErrorMessage = "Invalid bank account number format (26 digits or IBAN)")]
        public string? BankAccountNumber { get; set; }
    }
}