using MP.Domain.Booths;
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


        [Required]
        [Range(0.01, 9999.99)]
        [Display(Name = "Cena za dzień")]
        public decimal PricePerDay { get; set; }

        [Required]
        [Display(Name = "Waluta")]
        public Currency Currency { get; set; }

        [Display(Name = "Status")]
        public BoothStatus Status { get; set; }
    }
}
