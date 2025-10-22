using MP.Domain.Booths;
using MP.Application.Contracts.Booths;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP.Booths
{
    public class UpdateBoothDto
    {
        [Required]
        [StringLength(10, MinimumLength = 1)]
        [Display(Name = "Numer stanowiska")]
        public string Number { get; set; } = null!;

        /// <summary>
        /// Legacy price per day - kept for backward compatibility
        /// Use PricingPeriods for new multi-period pricing
        /// </summary>
        [Obsolete("Use PricingPeriods instead. This property is kept for backward compatibility.")]
        [Range(0.01, 9999.99)]
        [Display(Name = "Cena za dzień")]
        public decimal? PricePerDay { get; set; }

        /// <summary>
        /// Multi-period pricing configuration
        /// At least one pricing period is required
        /// </summary>
        [MinLength(1, ErrorMessage = "At least one pricing period is required")]
        public List<BoothPricingPeriodDto> PricingPeriods { get; set; } = new();

        [Display(Name = "Status")]
        public BoothStatus Status { get; set; }
    }
}
