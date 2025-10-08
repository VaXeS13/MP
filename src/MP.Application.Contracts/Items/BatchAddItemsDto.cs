using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MP.Items
{
    public class BatchAddItemsDto
    {
        [Required]
        public Guid SheetId { get; set; }

        [Required]
        public List<Guid> ItemIds { get; set; } = new();

        [Required]
        [Range(0, 100)]
        public decimal CommissionPercentage { get; set; }
    }
}
