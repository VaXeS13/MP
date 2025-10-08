using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace MP.Items
{
    public class ItemSheetDto : FullAuditedEntityDto<Guid>
    {
        public Guid UserId { get; set; }
        public Guid? RentalId { get; set; }
        public string? BoothNumber { get; set; }
        public string Status { get; set; } = null!;
        public List<ItemSheetItemDto> Items { get; set; } = new();

        public int TotalItemsCount { get; set; }
        public int SoldItemsCount { get; set; }
        public int ReclaimedItemsCount { get; set; }
    }
}
