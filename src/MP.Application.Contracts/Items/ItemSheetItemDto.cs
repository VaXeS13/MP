using System;
using Volo.Abp.Application.Dtos;

namespace MP.Items
{
    public class ItemSheetItemDto : FullAuditedEntityDto<Guid>
    {
        public Guid ItemSheetId { get; set; }
        public Guid ItemId { get; set; }
        public int ItemNumber { get; set; }
        public string? Barcode { get; set; }
        public decimal CommissionPercentage { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? SoldAt { get; set; }

        // Navigation
        public ItemDto? Item { get; set; }
    }
}
