using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP.Rentals
{
    public class SellItemDto
    {
        [Required]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "Cena sprzedaży")]
        public decimal SalePrice { get; set; }

        [Required]
        [Display(Name = "Data sprzedaży")]
        public DateTime SoldDate { get; set; } = DateTime.Now;
    }
}