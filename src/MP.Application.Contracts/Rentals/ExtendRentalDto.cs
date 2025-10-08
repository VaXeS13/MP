using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP.Rentals
{
    public class ExtendRentalDto
    {
        [Required]
        [Display(Name = "Nowa data zakończenia")]
        public DateTime NewEndDate { get; set; }
    }
}