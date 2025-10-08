using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Carts
{
    public class UpdateCartItemDto
    {
        [Required]
        public Guid BoothTypeId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}