using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Items
{
    public class AddItemToSheetDto
    {
        [Required]
        public Guid ItemId { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal CommissionPercentage { get; set; }
    }
}
