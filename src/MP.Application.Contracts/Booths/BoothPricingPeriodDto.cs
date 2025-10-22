using System.ComponentModel.DataAnnotations;

namespace MP.Application.Contracts.Booths
{
    /// <summary>
    /// DTO for booth pricing period configuration
    /// </summary>
    public class BoothPricingPeriodDto
    {
        /// <summary>
        /// Number of days in this pricing period (e.g., 1, 3, 7, 14, 30)
        /// </summary>
        [Required]
        [Range(1, 365, ErrorMessage = "Days must be between 1 and 365")]
        public int Days { get; set; }

        /// <summary>
        /// Price for this complete period (not per day)
        /// Example: 7 days for 30 PLN (not 30 PLN per day)
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal PricePerPeriod { get; set; }

        /// <summary>
        /// Calculated effective price per day for this period
        /// </summary>
        public decimal EffectivePricePerDay => PricePerPeriod / Days;
    }
}
