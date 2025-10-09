using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Items
{
    public class AddItemToSheetDto
    {
        [Required]
        public Guid ItemId { get; set; }

        [Range(0, 100)]
        public decimal CommissionPercentage { get; set; } = 0;
    }
}
