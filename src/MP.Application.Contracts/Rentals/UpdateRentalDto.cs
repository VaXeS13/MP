using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP.Rentals
{
    public class UpdateRentalDto
    {
        [Required]
        [Display(Name = "Data rozpoczęcia")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "Data zakończenia")]
        public DateTime EndDate { get; set; }

        [StringLength(1000)]
        [Display(Name = "Notatki")]
        public string? Notes { get; set; }
    }
}